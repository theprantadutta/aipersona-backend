using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Infrastructure.Services;

public interface IPersonaSeederService
{
    Task<PersonaSeederResult> SeedPersonasAsync(CancellationToken cancellationToken = default);
}

public record PersonaSeederResult(
    int TotalProcessed,
    int ImagesFound,
    int Created,
    int Updated,
    int Skipped,
    List<string> Errors);

public record PersonaData(
    string Name,
    string Bio,
    string Description,
    List<string> PersonalityTraits,
    string LanguageStyle,
    List<string> Expertise,
    List<string> Tags,
    string? WikipediaSearch);

public class PersonaSeederService : IPersonaSeederService
{
    private readonly IApplicationDbContext _context;
    private readonly IFileRunnerService _fileRunnerService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PersonaSeederService> _logger;
    private readonly IConfiguration _configuration;

    // API Configuration
    private readonly string _geminiApiKey;
    private readonly string _geminiModel;
    private readonly string? _freewayApiUrl;
    private readonly string? _freewayApiKey;
    private readonly string _adminEmail;
    private readonly string _adminPassword;

    // Rate limiting
    private const int RequestDelaySeconds = 5;
    private static readonly int[] RetryDelays = [60, 180, 300]; // 1min, 3min, 5min
    private static readonly int[] RateLimitStatusCodes = [429, 503, 500];

    public PersonaSeederService(
        IApplicationDbContext context,
        IFileRunnerService fileRunnerService,
        IPasswordHasher passwordHasher,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PersonaSeederService> logger)
    {
        _context = context;
        _fileRunnerService = fileRunnerService;
        _passwordHasher = passwordHasher;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _geminiApiKey = !string.IsNullOrEmpty(configuration["Gemini:ApiKey"])
            ? configuration["Gemini:ApiKey"]!
            : Environment.GetEnvironmentVariable("GEMINI_API_KEY")
              ?? throw new InvalidOperationException("Gemini API key is not configured");

        _geminiModel = !string.IsNullOrEmpty(configuration["Gemini:Model"])
            ? configuration["Gemini:Model"]!
            : Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.5-flash";

        _freewayApiUrl = !string.IsNullOrEmpty(configuration["Freeway:ApiUrl"])
            ? configuration["Freeway:ApiUrl"]
            : Environment.GetEnvironmentVariable("FREEWAY_API_URL");

        _freewayApiKey = !string.IsNullOrEmpty(configuration["Freeway:ApiKey"])
            ? configuration["Freeway:ApiKey"]
            : Environment.GetEnvironmentVariable("FREEWAY_API_KEY");

        _adminEmail = !string.IsNullOrEmpty(configuration["Admin:Email"])
            ? configuration["Admin:Email"]!
            : Environment.GetEnvironmentVariable("ADMIN_EMAIL")
              ?? throw new InvalidOperationException("Admin email is not configured");

        _adminPassword = !string.IsNullOrEmpty(configuration["Admin:Password"])
            ? configuration["Admin:Password"]!
            : Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
              ?? throw new InvalidOperationException("Admin password is not configured");
    }

    public async Task<PersonaSeederResult> SeedPersonasAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("======================================================================");
        _logger.LogInformation("AI Persona Database Seeding - From JSON with Wikipedia Images");
        _logger.LogInformation("======================================================================");

        var errors = new List<string>();
        var personasData = LoadPersonasData();
        _logger.LogInformation("Loaded {Count} personas from JSON", personasData.Count);

        // Get or create admin user
        var adminUser = await GetOrCreateAdminUserAsync(cancellationToken);

        int created = 0, updated = 0, skipped = 0, imagesFound = 0;

        for (int i = 0; i < personasData.Count; i++)
        {
            var persona = personasData[i];
            _logger.LogInformation("[{Current}/{Total}] Processing: {Name}", i + 1, personasData.Count, persona.Name);

            try
            {
                // Process image
                var imageUrl = await ProcessPersonaImageAsync(persona, cancellationToken);
                if (imageUrl != null) imagesFound++;

                // Seed or update persona
                var (wasCreated, wasUpdated) = await SeedOrUpdatePersonaAsync(adminUser, persona, imageUrl, cancellationToken);

                if (wasCreated) created++;
                else if (wasUpdated) updated++;
                else skipped++;

                // Rate limiting delay between personas
                if (i < personasData.Count - 1)
                {
                    _logger.LogDebug("Waiting {Seconds}s before next persona...", RequestDelaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(RequestDelaySeconds), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing persona {Name}", persona.Name);
                errors.Add($"{persona.Name}: {ex.Message}");
            }
        }

        var totalPersonas = await _context.Personas
            .CountAsync(p => p.CreatorId == adminUser.Id, cancellationToken);

        _logger.LogInformation("======================================================================");
        _logger.LogInformation("Seeding Complete!");
        _logger.LogInformation("   Total personas processed: {Count}", personasData.Count);
        _logger.LogInformation("   Images successfully fetched: {Count}", imagesFound);
        _logger.LogInformation("   New personas created: {Count}", created);
        _logger.LogInformation("   Existing personas updated: {Count}", updated);
        _logger.LogInformation("   Personas skipped (unchanged): {Count}", skipped);
        _logger.LogInformation("   Total personas in database: {Count}", totalPersonas);
        _logger.LogInformation("   Admin user: {Email}", adminUser.Email);
        _logger.LogInformation("======================================================================");

        return new PersonaSeederResult(personasData.Count, imagesFound, created, updated, skipped, errors);
    }

    private List<PersonaData> LoadPersonasData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "AiPersona.Infrastructure.Data.personas_data.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Fallback to file system
            var basePath = Path.GetDirectoryName(assembly.Location)!;
            var jsonPath = Path.Combine(basePath, "Data", "personas_data.json");

            if (!File.Exists(jsonPath))
            {
                // Try relative path from project
                jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AiPersona.Infrastructure", "Data", "personas_data.json");
            }

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException("personas_data.json not found");
            }

            var json = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<List<PersonaData>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return JsonSerializer.Deserialize<List<PersonaData>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    private async Task<User> GetOrCreateAdminUserAsync(CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == _adminEmail, cancellationToken);

        if (user != null)
        {
            _logger.LogInformation("Using existing admin user: {Email}", _adminEmail);
            if (!user.IsAdmin)
            {
                user.IsAdmin = true;
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated user to admin: {Email}", _adminEmail);
            }
            return user;
        }

        _logger.LogInformation("Creating admin user: {Email}", _adminEmail);

        user = new User
        {
            Email = _adminEmail,
            PasswordHash = _passwordHasher.HashPassword(_adminPassword),
            DisplayName = "AI Persona Admin",
            IsActive = true,
            IsAdmin = true,
            SubscriptionTier = SubscriptionTier.Lifetime,
            AuthProvider = AuthProvider.Email,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Create usage tracking
        var usage = new UsageTracking { UserId = user.Id };
        _context.UsageTrackings.Add(usage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created admin user: {Email}", _adminEmail);
        return user;
    }

    private async Task<string?> ProcessPersonaImageAsync(PersonaData persona, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[PROCESSING] {Name}", persona.Name);

        // Step 1: Get Wikipedia URL
        var wikiUrl = await GetWikipediaUrlAsync(
            persona.Name,
            persona.Bio,
            persona.WikipediaSearch ?? persona.Name,
            cancellationToken);

        if (string.IsNullOrEmpty(wikiUrl))
        {
            _logger.LogDebug("  [SKIP] Could not find Wikipedia URL for {Name}", persona.Name);
            return null;
        }

        await Task.Delay(500, cancellationToken); // Small delay

        // Step 2: Get image URL from Wikipedia
        var imageUrl = await GetWikipediaImageUrlAsync(wikiUrl, cancellationToken);
        if (string.IsNullOrEmpty(imageUrl))
        {
            _logger.LogDebug("  [SKIP] Could not find image on Wikipedia for {Name}", persona.Name);
            return null;
        }

        // Step 3: Download the image
        var imageData = await DownloadImageAsync(imageUrl, persona.Name, cancellationToken);
        if (imageData == null)
        {
            _logger.LogDebug("  [SKIP] Could not download image for {Name}", persona.Name);
            return null;
        }

        // Step 4: Upload to FileRunner
        try
        {
            var contentType = GetContentType(imageUrl);
            var extension = GetFileExtension(contentType);
            var filename = $"{SanitizeFilename(persona.Name)}{extension}";

            var result = await _fileRunnerService.UploadFileAsync(
                imageData,
                filename,
                contentType,
                "persona_image",
                cancellationToken);

            var fileUrl = _fileRunnerService.GetFileUrl(result.FileId);
            _logger.LogInformation("  [OK] Uploaded to FileRunner: {Url}", fileUrl);
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  [ERROR] FileRunner upload failed for {Name}", persona.Name);
            return null;
        }
    }

    private async Task<string?> GetWikipediaUrlAsync(
        string personaName,
        string personaBio,
        string searchHint,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("  [WIKI] Searching Wikipedia URL for: {Name}", personaName);

        var prompt = $"""
            You are a helpful research assistant. Your task is to find the official English Wikipedia page URL
            for a given person or character. If you find a direct and exact match, respond ONLY with the full URL
            (e.g., https://en.wikipedia.org/wiki/...). Do not include any other text, explanation, or formatting.
            Just the URL. If you are not certain or no page exists, respond with the single word: null

            Find the Wikipedia URL for: '{personaName}'
            Description: {personaBio}
            Additional context: {searchHint}
            """;

        var messages = new[]
        {
            new { role = "system", content = "You are a helpful research assistant. Your task is to find the official English Wikipedia page URL for a given person or character. If you find a direct and exact match, respond ONLY with the full URL (e.g., https://en.wikipedia.org/wiki/...). Do not include any other text, explanation, or formatting. Just the URL. If you are not certain or no page exists, respond with the single word: null" },
            new { role = "user", content = $"Find the Wikipedia URL for: '{personaName}'\nDescription: {personaBio}\nAdditional context: {searchHint}" }
        };

        int attempt = 0;
        int maxAttempts = RetryDelays.Length + 1;
        bool useFreeway = false;
        string? potentialUrl = null;

        while (attempt < maxAttempts)
        {
            try
            {
                if (!useFreeway)
                {
                    _logger.LogDebug("  [API] Attempting with Gemini ({Model})", _geminiModel);
                    var (success, url, shouldRetry) = await CallGeminiForUrlAsync(prompt, cancellationToken);

                    if (success)
                    {
                        potentialUrl = url;
                        break;
                    }

                    if (shouldRetry)
                    {
                        _logger.LogDebug("  [RATE LIMIT] Gemini rate limited. Trying Freeway...");
                        useFreeway = true;
                        continue;
                    }

                    useFreeway = true;
                    continue;
                }
                else
                {
                    if (string.IsNullOrEmpty(_freewayApiUrl) || string.IsNullOrEmpty(_freewayApiKey))
                    {
                        _logger.LogWarning("  [WARN] Freeway API not configured");
                        return null;
                    }

                    _logger.LogDebug("  [API] Attempting with Freeway (paid)");
                    var (success, url, shouldRetry) = await CallFreewayForUrlAsync(messages, cancellationToken);

                    if (success)
                    {
                        potentialUrl = url;
                        break;
                    }

                    if (shouldRetry && attempt < RetryDelays.Length)
                    {
                        var waitTime = RetryDelays[attempt];
                        _logger.LogDebug("  [RATE LIMIT] Waiting {Time}s before retry...", waitTime);
                        await Task.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken);
                        attempt++;
                        useFreeway = false;
                        continue;
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "  [ERROR] API error");
                if (!useFreeway)
                {
                    useFreeway = true;
                    continue;
                }
                return null;
            }
        }

        // Verify the URL
        if (string.IsNullOrEmpty(potentialUrl) || potentialUrl.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("  [WARN] AI did not find a Wikipedia URL for {Name}", personaName);
            return null;
        }

        // Clean up the URL
        potentialUrl = potentialUrl.Trim('`', ' ', '\n', '\r');

        // Handle markdown link format [text](url)
        if (potentialUrl.StartsWith('[') && potentialUrl.Contains("]("))
        {
            var match = Regex.Match(potentialUrl, @"\]\(([^)]+)\)");
            if (match.Success)
            {
                potentialUrl = match.Groups[1].Value;
            }
        }

        if (!potentialUrl.StartsWith("https://en.wikipedia.org/wiki/"))
        {
            _logger.LogDebug("  [WARN] Invalid URL format for {Name}: {Url}", personaName, potentialUrl);
            return null;
        }

        _logger.LogDebug("  [OK] Got Wikipedia URL: {Url}", potentialUrl);
        return potentialUrl;
    }

    private async Task<(bool Success, string? Url, bool ShouldRetry)> CallGeminiForUrlAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiModel}:generateContent?key={_geminiApiKey}";

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.1, maxOutputTokens = 256 }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

        if (RateLimitStatusCodes.Contains((int)response.StatusCode))
        {
            return (false, null, true);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (false, null, false);
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()?.Trim();

        _logger.LogDebug("  [OK] Response received from Gemini");
        return (true, text, false);
    }

    private async Task<(bool Success, string? Url, bool ShouldRetry)> CallFreewayForUrlAsync(
        object[] messages,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = "paid",
            messages,
            temperature = 0.1,
            max_tokens = 256
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_freewayApiUrl}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Api-Key", _freewayApiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (RateLimitStatusCodes.Contains((int)response.StatusCode))
        {
            return (false, null, true);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (false, null, false);
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim();

        _logger.LogDebug("  [OK] Response received from Freeway (paid)");
        return (true, text, false);
    }

    private async Task<string?> GetWikipediaImageUrlAsync(string wikiUrl, CancellationToken cancellationToken)
    {
        try
        {
            var pageTitle = wikiUrl.Split("/wiki/").Last();
            var apiUrl = $"https://en.wikipedia.org/w/api.php?action=query&titles={pageTitle}&prop=pageimages&format=json&pithumbsize=500";

            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("User-Agent", "AIPersonaBot/1.0 (https://aipersona.app; contact@aipersona.app)");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("  [WARN] Wikipedia API failed: {StatusCode}", response.StatusCode);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            var pages = doc.RootElement.GetProperty("query").GetProperty("pages");
            foreach (var page in pages.EnumerateObject())
            {
                if (page.Name == "-1") continue;

                if (page.Value.TryGetProperty("thumbnail", out var thumbnail))
                {
                    var imageUrl = thumbnail.GetProperty("source").GetString();
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _logger.LogDebug("  [OK] Found Wikipedia image");
                        return imageUrl;
                    }
                }
            }

            _logger.LogDebug("  [WARN] No image found on Wikipedia page");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  [ERROR] Failed to get Wikipedia image");
            return null;
        }
    }

    private async Task<byte[]?> DownloadImageAsync(string imageUrl, string personaName, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
            request.Headers.Add("User-Agent", "AIPersonaBot/1.0 (https://aipersona.app; contact@aipersona.app) .NET/httpx");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/apng"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Referer", "https://en.wikipedia.org/");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (contentType.Contains("image"))
                {
                    var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    _logger.LogDebug("  [OK] Downloaded image ({Size} bytes)", data.Length);
                    return data;
                }
                else
                {
                    _logger.LogWarning("  [WARN] Response is not an image: {ContentType}", contentType);
                    return null;
                }
            }
            else
            {
                _logger.LogWarning("  [WARN] Failed to download image: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  [ERROR] Image download error for {Name}", personaName);
            return null;
        }
    }

    private async Task<(bool Created, bool Updated)> SeedOrUpdatePersonaAsync(
        User user,
        PersonaData personaData,
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Personas
            .FirstOrDefaultAsync(p => p.Name == personaData.Name && p.CreatorId == user.Id, cancellationToken);

        if (existing != null)
        {
            bool updated = false;

            // Update image if we have a new one and it's different
            if (!string.IsNullOrEmpty(imageUrl) && existing.ImagePath != imageUrl)
            {
                existing.ImagePath = imageUrl;
                updated = true;
            }

            // Update other fields if they differ
            if (existing.Bio != personaData.Bio)
            {
                existing.Bio = personaData.Bio;
                updated = true;
            }
            if (existing.Description != personaData.Description)
            {
                existing.Description = personaData.Description;
                updated = true;
            }
            if (!AreListsEqual(existing.PersonalityTraits, personaData.PersonalityTraits))
            {
                existing.PersonalityTraits = personaData.PersonalityTraits;
                updated = true;
            }
            if (existing.LanguageStyle != personaData.LanguageStyle)
            {
                existing.LanguageStyle = personaData.LanguageStyle;
                updated = true;
            }
            if (!AreListsEqual(existing.Expertise, personaData.Expertise))
            {
                existing.Expertise = personaData.Expertise;
                updated = true;
            }
            if (!AreListsEqual(existing.Tags, personaData.Tags))
            {
                existing.Tags = personaData.Tags;
                updated = true;
            }

            if (updated)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("  [UPDATE] Updated persona: {Name}", personaData.Name);
                return (false, true);
            }
            else
            {
                _logger.LogDebug("  [SKIP] Persona unchanged: {Name}", personaData.Name);
                return (false, false);
            }
        }
        else
        {
            // Create new persona with random initial stats
            var random = new Random();
            var persona = new Persona
            {
                CreatorId = user.Id,
                Name = personaData.Name,
                Description = personaData.Description,
                Bio = personaData.Bio,
                ImagePath = imageUrl,
                PersonalityTraits = personaData.PersonalityTraits,
                LanguageStyle = personaData.LanguageStyle,
                Expertise = personaData.Expertise,
                Tags = personaData.Tags,
                IsPublic = true,
                IsMarketplace = false,
                Status = PersonaStatus.Active,
                ConversationCount = random.Next(500, 5001),
                CloneCount = random.Next(50, 501),
                LikeCount = random.Next(1000, 10001),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Personas.Add(persona);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("  [CREATE] Created persona: {Name}", personaData.Name);
            return (true, false);
        }
    }

    private static bool AreListsEqual(List<string>? list1, List<string>? list2)
    {
        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;
        return list1.SequenceEqual(list2);
    }

    private static string SanitizeFilename(string name)
    {
        var safeName = Regex.Replace(name, @"[^\w\s-]", "");
        safeName = Regex.Replace(safeName, @"[-\s]+", "_");
        return safeName;
    }

    private static string GetContentType(string imageUrl)
    {
        var urlLower = imageUrl.ToLowerInvariant();
        if (urlLower.Contains(".png")) return "image/png";
        if (urlLower.Contains(".gif")) return "image/gif";
        if (urlLower.Contains(".webp")) return "image/webp";
        return "image/jpeg";
    }

    private static string GetFileExtension(string contentType)
    {
        return contentType switch
        {
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
    }
}
