using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class ContentReport : BaseEntity
{
    public Guid ReporterId { get; set; }
    public string ContentId { get; set; } = null!;
    public ContentType ContentType { get; set; }
    public ReportReason Reason { get; set; }
    public string? AdditionalInfo { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Admin review fields
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? Resolution { get; set; }

    // Navigation property
    public User Reporter { get; set; } = null!;
}
