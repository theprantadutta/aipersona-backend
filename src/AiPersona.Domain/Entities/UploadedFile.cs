using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class UploadedFile : BaseEntity
{
    public Guid UserId { get; set; }

    // File information
    public string FilePath { get; set; } = null!;
    public string OriginalName { get; set; } = null!;
    public int FileSize { get; set; }
    public string MimeType { get; set; } = null!;

    // Category
    public FileCategory Category { get; set; }

    // Reference to what it's attached to
    public FileReferenceType? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
