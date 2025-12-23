using System.Text.Json;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Enums;

namespace AiPersona.Infrastructure.Services;

public class GooglePlayService : IGooglePlayService
{
    private readonly ILogger<GooglePlayService> _logger;
    private readonly AndroidPublisherService? _publisherService;
    private readonly string _packageName;

    public GooglePlayService(IConfiguration configuration, ILogger<GooglePlayService> logger)
    {
        _logger = logger;

        var serviceAccountPath = configuration["GooglePlay:ServiceAccountPath"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_PLAY_SERVICE_ACCOUNT")
            ?? "google-play-service-account.json";

        _packageName = configuration["GooglePlay:PackageName"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_PLAY_PACKAGE_NAME")
            ?? throw new InvalidOperationException("Google Play package name is not configured");

        try
        {
            if (File.Exists(serviceAccountPath))
            {
                var credential = GoogleCredential.FromFile(serviceAccountPath)
                    .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

                _publisherService = new AndroidPublisherService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "AiPersona"
                });

                _logger.LogInformation("Google Play service initialized successfully");
            }
            else
            {
                _logger.LogWarning("Google Play service account file not found: {Path}", serviceAccountPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Play service");
        }
    }

    public async Task<SubscriptionVerificationResult> VerifySubscriptionAsync(
        string purchaseToken,
        string productId,
        CancellationToken cancellationToken = default)
    {
        if (_publisherService == null)
        {
            _logger.LogWarning("Google Play service not initialized");
            return new SubscriptionVerificationResult(false, SubscriptionTier.Free, DateTime.UtcNow, null, null);
        }

        try
        {
            var subscription = await _publisherService.Purchases.Subscriptions
                .Get(_packageName, productId, purchaseToken)
                .ExecuteAsync(cancellationToken);

            var expiryTimeMillis = subscription.ExpiryTimeMillis ?? 0L;
            var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiryTimeMillis).UtcDateTime;

            var tier = MapProductIdToTier(productId);
            var isValid = expiresAt > DateTime.UtcNow;

            var rawResponse = JsonSerializer.Serialize(subscription);

            _logger.LogInformation("Subscription verified: ProductId={ProductId}, Valid={IsValid}, ExpiresAt={ExpiresAt}",
                productId, isValid, expiresAt);

            return new SubscriptionVerificationResult(isValid, tier, expiresAt, productId, rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify subscription: {ProductId}", productId);
            return new SubscriptionVerificationResult(false, SubscriptionTier.Free, DateTime.UtcNow, null, null);
        }
    }

    private static SubscriptionTier MapProductIdToTier(string productId)
    {
        return productId.ToLowerInvariant() switch
        {
            var id when id.Contains("pro") => SubscriptionTier.Pro,
            var id when id.Contains("premium") => SubscriptionTier.Premium,
            var id when id.Contains("basic") => SubscriptionTier.Basic,
            _ => SubscriptionTier.Free
        };
    }
}
