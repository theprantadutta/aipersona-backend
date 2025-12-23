using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }

    // File information
    public string FilePath { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public int FileSize { get; set; }
    public string MimeType { get; set; } = null!;

    public AttachmentType AttachmentType { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ChatMessage Message { get; set; } = null!;
}
