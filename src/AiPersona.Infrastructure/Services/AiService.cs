using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Infrastructure.Services;

public class AiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiService> _logger;
    private readonly int _maxTokens;
    private readonly double _temperature;
    private readonly int _maxConversationHistory;

    // Freeway API configuration
    private readonly string _freewayApiUrl;
    private readonly string _freewayApiKey;
    private readonly string _freewayModel;

    public AiService(HttpClient httpClient, IConfiguration configuration, ILogger<AiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _maxTokens = int.Parse(configuration["AiDefaults:MaxTokens"] ?? "8192");
        _temperature = double.Parse(configuration["AiDefaults:Temperature"] ?? "0.7");
        _maxConversationHistory = int.Parse(configuration["AiDefaults:MaxConversationHistory"] ?? "20");

        // Freeway configuration - required
        var freewayUrl = configuration["Freeway:ApiUrl"];
        _freewayApiUrl = !string.IsNullOrEmpty(freewayUrl)
            ? freewayUrl
            : Environment.GetEnvironmentVariable("FREEWAY_API_URL")
              ?? throw new InvalidOperationException("FREEWAY_API_URL is not configured");

        var freewayKey = configuration["Freeway:ApiKey"];
        _freewayApiKey = !string.IsNullOrEmpty(freewayKey)
            ? freewayKey
            : Environment.GetEnvironmentVariable("FREEWAY_API_KEY")
              ?? throw new InvalidOperationException("FREEWAY_API_KEY is not configured");

        var freewayModel = configuration["Freeway:Model"];
        _freewayModel = !string.IsNullOrEmpty(freewayModel)
            ? freewayModel
            : Environment.GetEnvironmentVariable("FREEWAY_MODEL") ?? "free";

        _logger.LogInformation("AiService initialized with Freeway API at {Url}, model: {Model}",
            _freewayApiUrl, _freewayModel);
    }

    public async Task<AiResponse> GenerateResponseAsync(
        Persona persona,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(persona);
        var messages = BuildMessages(conversationHistory, userMessage);

        try
        {
            return await CallApiAsync(systemPrompt, messages, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Freeway API failed. Status: {Status}, URL: {Url}",
                ex.StatusCode, _freewayApiUrl);

            var errorType = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests => "rate_limit",
                System.Net.HttpStatusCode.ServiceUnavailable => "service_unavailable",
                System.Net.HttpStatusCode.BadGateway => "service_unavailable",
                System.Net.HttpStatusCode.GatewayTimeout => "timeout",
                _ => "unknown"
            };

            return new AiResponse(
                "I apologize, but I'm having trouble connecting right now. Please try again in a moment.",
                TokensUsed: 0,
                IsFallback: true,
                ErrorType: errorType);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Freeway API request timed out");

            return new AiResponse(
                "I apologize, but the request took too long. Please try again.",
                TokensUsed: 0,
                IsFallback: true,
                ErrorType: "timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Freeway API");

            return new AiResponse(
                "I apologize, but something went wrong. Please try again.",
                TokensUsed: 0,
                IsFallback: true,
                ErrorType: "unknown");
        }
    }

    private async Task<AiResponse> CallApiAsync(
        string systemPrompt,
        List<(string role, string content)> messages,
        CancellationToken cancellationToken)
    {
        var url = _freewayApiUrl.TrimEnd('/') + "/chat/completions";

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

        return new AiResponse(text, tokensUsed);
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

        sb.AppendLine("\nGuidelines:");
        sb.AppendLine("- Keep responses SHORT and concise (1-3 sentences) unless asked for details");
        sb.AppendLine("- Only give long explanations when user explicitly asks (e.g., 'explain', 'describe', 'tell me more')");
        sb.AppendLine("- Be natural, helpful, and stay in character");

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
}
