using AiPersona.Domain.Common;

namespace AiPersona.Domain.Entities;

public class UsageTracking : AuditableEntity
{
    public Guid UserId { get; set; }

    // Message limits
    public int MessagesToday { get; set; }
    public DateTime MessagesCountResetAt { get; set; } = DateTime.UtcNow;

    // Persona limits
    public int PersonasCount { get; set; }

    // Storage tracking
    public long StorageUsedBytes { get; set; }

    // API usage tracking
    public int GeminiApiCallsToday { get; set; }
    public long GeminiTokensUsedTotal { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
