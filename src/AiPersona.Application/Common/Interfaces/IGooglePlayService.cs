using AiPersona.Domain.Enums;

namespace AiPersona.Application.Common.Interfaces;

public record SubscriptionVerificationResult(
    bool IsValid,
    SubscriptionTier Tier,
    DateTime ExpiresAt,
    string? ProductId,
    string? RawResponse);

public interface IGooglePlayService
{
    Task<SubscriptionVerificationResult> VerifySubscriptionAsync(string purchaseToken, string productId, CancellationToken cancellationToken = default);
}
