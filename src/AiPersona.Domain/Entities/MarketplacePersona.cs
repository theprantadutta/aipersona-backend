using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class MarketplacePersona : AuditableEntity
{
    public Guid PersonaId { get; set; }
    public Guid SellerId { get; set; }

    // Listing information
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;

    // Pricing
    public PricingType PricingType { get; set; }
    public decimal Price { get; set; }

    // Status
    public MarketplaceStatus Status { get; set; } = MarketplaceStatus.Pending;

    // Analytics
    public int ViewCount { get; set; }
    public int PurchaseCount { get; set; }

    // Approval
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public Persona Persona { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public ICollection<MarketplacePurchase> Purchases { get; set; } = new List<MarketplacePurchase>();
    public ICollection<MarketplaceReview> Reviews { get; set; } = new List<MarketplaceReview>();
}
