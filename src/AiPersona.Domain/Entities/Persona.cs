using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class Persona : AuditableEntity
{
    public Guid CreatorId { get; set; }

    // Basic info
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public string? Bio { get; set; }

    // Personality configuration (stored as JSON)
    public List<string>? PersonalityTraits { get; set; }
    public string? LanguageStyle { get; set; }
    public List<string>? Expertise { get; set; }
    public List<string>? Tags { get; set; }

    // Voice settings
    public string? VoiceId { get; set; }
    public string? VoiceSettings { get; set; }

    // Status and visibility
    public PersonaStatus Status { get; set; } = PersonaStatus.Active;
    public bool IsPublic { get; set; } = true;
    public bool IsMarketplace { get; set; }

    // AI Configuration
    public string? Prompt { get; set; }

    // Analytics
    public int ConversationCount { get; set; }
    public int CloneCount { get; set; }
    public int LikeCount { get; set; }
    public int FavoriteCount { get; set; }
    public int ViewCount { get; set; }

    // Cloning support
    public Guid? ClonedFromPersonaId { get; set; }
    public Guid? OriginalCreatorId { get; set; }

    // Navigation properties
    public User Creator { get; set; } = null!;
    public User? OriginalCreator { get; set; }
    public Persona? ClonedFromPersona { get; set; }
    public ICollection<KnowledgeBase> KnowledgeBases { get; set; } = new List<KnowledgeBase>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public MarketplacePersona? MarketplaceListing { get; set; }
    public ICollection<PersonaLike> Likes { get; set; } = new List<PersonaLike>();
    public ICollection<PersonaFavorite> Favorites { get; set; } = new List<PersonaFavorite>();
    public ICollection<PersonaView> Views { get; set; } = new List<PersonaView>();
}
