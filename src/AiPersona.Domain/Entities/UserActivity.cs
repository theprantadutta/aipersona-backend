using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class UserActivity : BaseEntity
{
    public Guid UserId { get; set; }
    public ActivityType ActivityType { get; set; }
    public Guid? TargetId { get; set; }
    public ContentType? TargetType { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }  // JSON serialized metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
