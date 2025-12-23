using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class SubscriptionEvent : BaseEntity
{
    public Guid UserId { get; set; }

    // Google Play purchase information
    public string PurchaseToken { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public SubscriptionTier SubscriptionTier { get; set; }

    // Expiration
    public DateTime ExpiresAt { get; set; }

    // Event tracking
    public SubscriptionEventType EventType { get; set; }
    public VerificationStatus VerificationStatus { get; set; }

    // Raw Google Play response (JSON)
    public string? RawResponse { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
