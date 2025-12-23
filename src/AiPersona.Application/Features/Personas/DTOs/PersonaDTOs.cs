namespace AiPersona.Application.Features.Personas.DTOs;

public record PersonaDto(
    Guid Id,
    Guid CreatorId,
    string Name,
    string? Description,
    string? Bio,
    string? ImagePath,
    List<string>? PersonalityTraits,
    string? LanguageStyle,
    List<string>? Expertise,
    List<string>? Tags,
    string? VoiceId,
    string? VoiceSettings,
    bool IsPublic,
    bool IsMarketplace,
    string Status,
    int LikeCount,
    int ViewCount,
    int CloneCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsOwner = false,
    bool IsLiked = false,
    string? CreatorName = null);

public record PersonaListDto(
    List<PersonaDto> Personas,
    int Total,
    int Page,
    int PageSize);

public record TrendingPersonasDto(
    List<PersonaDto> Personas,
    string Timeframe);

public record KnowledgeBaseDto(
    Guid Id,
    Guid PersonaId,
    string SourceType,
    string? SourceName,
    string? Content,
    string Status,
    string? MetaData,
    DateTime CreatedAt);

public record CreatePersonaDto(
    string Name,
    string? Description = null,
    string? Bio = null,
    List<string>? PersonalityTraits = null,
    string? LanguageStyle = null,
    List<string>? Expertise = null,
    List<string>? Tags = null,
    string? VoiceId = null,
    string? VoiceSettings = null,
    bool IsPublic = false,
    bool IsMarketplace = false);

public record UpdatePersonaDto(
    string? Name = null,
    string? Description = null,
    string? Bio = null,
    string? ImagePath = null,
    List<string>? PersonalityTraits = null,
    string? LanguageStyle = null,
    List<string>? Expertise = null,
    List<string>? Tags = null,
    string? VoiceId = null,
    string? VoiceSettings = null,
    bool? IsPublic = null,
    bool? IsMarketplace = null,
    string? Status = null);
