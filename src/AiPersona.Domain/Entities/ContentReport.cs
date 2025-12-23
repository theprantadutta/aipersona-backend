using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class ContentReport : BaseEntity
{
    public Guid ReporterId { get; set; }
    public Guid ContentId { get; set; }
    public ContentType ContentType { get; set; }
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Admin review fields
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedById { get; set; }
    public string? Resolution { get; set; }

    // Navigation properties
    public User Reporter { get; set; } = null!;
    public User? ResolvedBy { get; set; }
}
