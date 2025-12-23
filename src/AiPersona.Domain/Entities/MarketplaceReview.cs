using AiPersona.Domain.Common;

namespace AiPersona.Domain.Entities;

public class MarketplaceReview : AuditableEntity
{
    public Guid MarketplacePersonaId { get; set; }
    public Guid ReviewerId { get; set; }

    // Review
    public int Rating { get; set; }
    public string? ReviewText { get; set; }

    // Navigation properties
    public MarketplacePersona MarketplacePersona { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}
