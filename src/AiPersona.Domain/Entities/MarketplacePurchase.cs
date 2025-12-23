using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class MarketplacePurchase : BaseEntity
{
    public Guid BuyerId { get; set; }
    public Guid MarketplacePersonaId { get; set; }

    // Purchase details
    public decimal Amount { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Completed;

    // Timestamp
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Buyer { get; set; } = null!;
    public MarketplacePersona MarketplacePersona { get; set; } = null!;
}
