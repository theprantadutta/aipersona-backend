using AiPersona.Domain.Common;

namespace AiPersona.Domain.Entities;

public class UserBlock : BaseEntity
{
    public Guid BlockerId { get; set; }
    public Guid BlockedId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    // Navigation properties
    public User Blocker { get; set; } = null!;
    public User Blocked { get; set; } = null!;
}
