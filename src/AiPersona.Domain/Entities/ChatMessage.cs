using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid SessionId { get; set; }

    public Guid SenderId { get; set; }
    public SenderType SenderType { get; set; }

    // Message content
    public string Text { get; set; } = null!;
    public MessageType MessageType { get; set; } = MessageType.Text;

    // AI-specific fields
    public string? Sentiment { get; set; }
    public int TokensUsed { get; set; }

    // Metadata (JSON)
    public string? MetaData { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ChatSession Session { get; set; } = null!;
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}
