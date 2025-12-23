using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class AutomatedResponse : AuditableEntity
{
    public TicketCategory Category { get; set; }

    // Response content
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;

    // Keywords for matching
    public string? Keywords { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Priority for matching
    public int Priority { get; set; }
}
