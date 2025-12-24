using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Infrastructure.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;
    private readonly int _maxConversationHistory;

    // Fallback to Freeway API
    private readonly string? _freewayApiUrl;
    private readonly string? _freewayApiKey;
    private readonly string? _freewayModel;

    // Retry configuration
    private const int MaxRetries = 3;
    private static readonly int[] RetryDelaysMs = [1000, 2000, 4000];

    public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Use !string.IsNullOrEmpty to handle empty strings from config
        var geminiApiKey = configuration["Gemini:ApiKey"];
        _apiKey = !string.IsNullOrEmpty(geminiApiKey)
            ? geminiApiKey
            : Environment.GetEnvironmentVariable("GEMINI_API_KEY")
              ?? throw new InvalidOperationException("Gemini API key is not configured");

        var geminiModel = configuration["Gemini:Model"];
        _model = !string.IsNullOrEmpty(geminiModel)
            ? geminiModel
            : Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.0-flash-exp";

        _maxTokens = int.Parse(configuration["AiDefaults:MaxTokens"] ?? "8192");
        _temperature = double.Parse(configuration["AiDefaults:Temperature"] ?? "0.7");
        _maxConversationHistory = int.Parse(configuration["AiDefaults:MaxConversationHistory"] ?? "20");

        // Freeway fallback - handle empty strings from config
        var freewayUrl = configuration["Freeway:ApiUrl"];
        _freewayApiUrl = !string.IsNullOrEmpty(freewayUrl)
            ? freewayUrl
            : Environment.GetEnvironmentVariable("FREEWAY_API_URL");

        var freewayKey = configuration["Freeway:ApiKey"];
        _freewayApiKey = !string.IsNullOrEmpty(freewayKey)
            ? freewayKey
            : Environment.GetEnvironmentVariable("FREEWAY_API_KEY");

        var freewayModel = configuration["Freeway:Model"];
        _freewayModel = !string.IsNullOrEmpty(freewayModel)
            ? freewayModel
            : Environment.GetEnvironmentVariable("FREEWAY_MODEL");

        _logger.LogInformation("GeminiService initialized. Freeway fallback configured: {HasFreeway}",
            !string.IsNullOrEmpty(_freewayApiUrl) && !string.IsNullOrEmpty(_freewayApiKey));
    }

    public async Task<GeminiResponse> GenerateResponseAsync(
        Persona persona,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(persona);
        var messages = BuildMessages(conversationHistory, userMessage);

        // Try Gemini API with retry logic for rate limit (429) errors
        Exception? lastGeminiException = null;
        string? errorType = null;
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await CallGeminiApiAsync(systemPrompt, messages, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                lastGeminiException = ex;
                errorType = "rate_limit";
                if (attempt < MaxRetries - 1)
                {
                    var delay = RetryDelaysMs[attempt];
                    _logger.LogWarning("Gemini API rate limited (429). Retry {Attempt}/{MaxRetries} after {Delay}ms",
                        attempt + 1, MaxRetries, delay);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastGeminiException = ex;
                errorType = "timeout";
                _logger.LogWarning(ex, "Gemini API call timed out on attempt {Attempt}", attempt + 1);
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                lastGeminiException = ex;
                errorType = "service_unavailable";
                _logger.LogWarning(ex, "Gemini API service unavailable on attempt {Attempt}", attempt + 1);
                break;
            }
            catch (Exception ex)
            {
                lastGeminiException = ex;
                errorType = "unknown";
                _logger.LogWarning(ex, "Gemini API call failed on attempt {Attempt}", attempt + 1);
                break; // Don't retry non-429 errors
            }
        }

        // Try Freeway fallback
        var hasFreewayConfig = !string.IsNullOrEmpty(_freewayApiUrl) && !string.IsNullOrEmpty(_freewayApiKey);
        _logger.LogWarning(lastGeminiException, "Gemini API failed after retries. Freeway fallback available: {HasFreeway}", hasFreewayConfig);

        if (hasFreewayConfig)
        {
            _logger.LogInformation("Attempting Freeway API fallback to {Url}", _freewayApiUrl);
            try
            {
                return await CallFreewayApiAsync(systemPrompt, messages, cancellationToken);
            }
            catch (HttpRequestException freewayEx)
            {
                _logger.LogError(freewayEx, "Freeway API fallback failed. Status: {StatusCode}, URL: {Url}",
                    freewayEx.StatusCode, _freewayApiUrl + "/chat/completions");

                // Return a graceful fallback response instead of throwing
                _logger.LogWarning("All AI APIs failed. Returning fallback response.");
                return new GeminiResponse(
                    "I apologize, but I'm having trouble connecting right now. Please try again in a moment.",
                    TokensUsed: 0,
                    IsFallback: true,
                    ErrorType: errorType ?? "service_unavailable");
            }
            catch (Exception freewayEx)
            {
                _logger.LogError(freewayEx, "Freeway API fallback failed unexpectedly");

                // Return a graceful fallback response
                return new GeminiResponse(
                    "I apologize, but I'm having trouble connecting right now. Please try again in a moment.",
                    TokensUsed: 0,
                    IsFallback: true,
                    ErrorType: errorType ?? "unknown");
            }
        }

        _logger.LogError("No Freeway fallback configured. Set FREEWAY_API_URL and FREEWAY_API_KEY environment variables.");

        // Return graceful fallback instead of throwing
        return new GeminiResponse(
            "I apologize, but I'm temporarily unavailable. Please try again in a moment.",
            TokensUsed: 0,
            IsFallback: true,
            ErrorType: errorType ?? "service_unavailable");
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        Persona persona,
        List<ChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(persona);
        var messages = BuildMessages(conversationHistory, userMessage);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:streamGenerateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = messages.Select(m => new
            {
                role = m.role,
                parts = new[] { new { text = m.content } }
            }).ToArray(),
            systemInstruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            generationConfig = new
            {
                temperature = _temperature,
                maxOutputTokens = _maxTokens
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            // Parse SSE data
            if (line.StartsWith("data: "))
            {
                var data = line[6..];
                if (data == "[DONE]") break;

                var text = TryParseStreamChunk(data);
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
    }

    private async Task<GeminiResponse> CallGeminiApiAsync(
        string systemPrompt,
        List<(string role, string content)> messages,
        CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = messages.Select(m => new
            {
                role = m.role,
                parts = new[] { new { text = m.content } }
            }).ToArray(),
            systemInstruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            generationConfig = new
            {
                temperature = _temperature,
                maxOutputTokens = _maxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";

        var tokensUsed = doc.RootElement
            .TryGetProperty("usageMetadata", out var usage)
            ? usage.GetProperty("totalTokenCount").GetInt32()
            : 0;

        return new GeminiResponse(text, tokensUsed);
    }

    private async Task<GeminiResponse> CallFreewayApiAsync(
        string systemPrompt,
        List<(string role, string content)> messages,
        CancellationToken cancellationToken)
    {
        // Ensure we have the full endpoint URL
        var url = _freewayApiUrl!.TrimEnd('/') + "/chat/completions";

        var openAiMessages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var m in messages)
        {
            openAiMessages.Add(new { role = m.role == "user" ? "user" : "assistant", content = m.content });
        }

        var requestBody = new
        {
            model = _freewayModel,
            messages = openAiMessages,
            temperature = _temperature,
            max_tokens = _maxTokens
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Api-Key", _freewayApiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        var tokensUsed = doc.RootElement
            .TryGetProperty("usage", out var usage)
            ? usage.GetProperty("total_tokens").GetInt32()
            : 0;

        return new GeminiResponse(text, tokensUsed);
    }

    private string BuildSystemPrompt(Persona persona)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are {persona.Name}.");

        if (!string.IsNullOrEmpty(persona.Description))
            sb.AppendLine($"Description: {persona.Description}");

        if (!string.IsNullOrEmpty(persona.Bio))
            sb.AppendLine($"Bio: {persona.Bio}");

        if (persona.PersonalityTraits?.Count > 0)
            sb.AppendLine($"Personality traits: {string.Join(", ", persona.PersonalityTraits)}");

        if (!string.IsNullOrEmpty(persona.LanguageStyle))
            sb.AppendLine($"Communication style: {persona.LanguageStyle}");

        if (persona.Expertise?.Count > 0)
            sb.AppendLine($"Areas of expertise: {string.Join(", ", persona.Expertise)}");

        // Add knowledge base content if available
        if (persona.KnowledgeBases?.Count > 0)
        {
            sb.AppendLine("\nKnowledge Base:");
            foreach (var kb in persona.KnowledgeBases.Where(k => k.Status == KnowledgeStatus.Active))
            {
                sb.AppendLine($"- {kb.Content}");
            }
        }

        sb.AppendLine("\nRespond naturally in character. Be helpful, engaging, and consistent with your personality.");

        return sb.ToString();
    }

    private List<(string role, string content)> BuildMessages(List<ChatMessage> history, string userMessage)
    {
        var messages = new List<(string role, string content)>();

        // Take the last N messages for context
        var recentHistory = history
            .OrderByDescending(m => m.CreatedAt)
            .Take(_maxConversationHistory)
            .Reverse()
            .ToList();

        foreach (var msg in recentHistory)
        {
            var role = msg.SenderType == SenderType.User ? "user" : "model";
            messages.Add((role, msg.Text));
        }

        // Add the current user message
        messages.Add(("user", userMessage));

        return messages;
    }

    private string? TryParseStreamChunk(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse streaming response chunk");
            return null;
        }
    }
}
