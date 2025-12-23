using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class KnowledgeBase : AuditableEntity
{
    public Guid PersonaId { get; set; }

    // Source information
    public KnowledgeSourceType SourceType { get; set; }
    public string? SourceName { get; set; }
    public string Content { get; set; } = null!;

    // Processing status
    public int Tokens { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.Active;
    public DateTime? IndexedAt { get; set; }

    // Additional metadata (JSON)
    public string? MetaData { get; set; }

    // Navigation property
    public Persona Persona { get; set; } = null!;
}
