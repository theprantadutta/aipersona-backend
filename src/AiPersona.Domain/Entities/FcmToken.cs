using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class FcmToken : BaseEntity
{
    public Guid UserId { get; set; }

    // FCM token
    public string Token { get; set; } = null!;

    // Device information
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public Platform Platform { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
