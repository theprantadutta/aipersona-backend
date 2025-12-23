using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class ChatSession : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? PersonaId { get; set; }

    public string PersonaName { get; set; } = null!;

    // Deleted persona tracking
    public string? DeletedPersonaName { get; set; }
    public string? DeletedPersonaImage { get; set; }
    public DateTime? PersonaDeletedAt { get; set; }

    // Status
    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Active;
    public bool IsPinned { get; set; }

    // Metadata
    public int MessageCount { get; set; }
    public string? MetaData { get; set; }

    // Timestamps
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Persona? Persona { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
