namespace AiPersona.Application.Features.Chat.DTOs;

public record ChatSessionDto(
    Guid Id,
    Guid UserId,
    Guid? PersonaId,
    string PersonaName,
    string? PersonaImageUrl,
    string? Title,
    string Status,
    bool IsPinned,
    int MessageCount,
    string? LastMessage,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    DateTime UpdatedAt,
    bool IsPersonaDeleted = false,
    string? DeletedPersonaName = null,
    string? DeletedPersonaImage = null,
    DateTime? PersonaDeletedAt = null,
    Dictionary<string, object>? Settings = null,
    List<string>? Tags = null);

public record ChatSessionDetailDto(
    Guid Id,
    Guid UserId,
    Guid? PersonaId,
    string PersonaName,
    string? PersonaImageUrl,
    string? Title,
    string Status,
    bool IsPinned,
    int MessageCount,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    DateTime UpdatedAt,
    List<ChatMessageDto> Messages);

public record ChatSessionListDto(
    List<ChatSessionDto> Sessions,
    int Total,
    int Page,
    int PageSize);

public record ChatSessionSearchDto(
    List<ChatSessionDto> Sessions,
    int Total,
    int Page,
    int PageSize,
    string? Query,
    List<string> FiltersApplied);

public record ChatMessageDto(
    Guid Id,
    Guid SessionId,
    Guid? SenderId,
    string SenderType,
    string Text,
    string? MessageType,
    int? TokensUsed,
    DateTime CreatedAt,
    Dictionary<string, object>? Metadata = null,
    List<MessageAttachmentDto>? Attachments = null);

public record MessageAttachmentDto(
    Guid Id,
    string FileType,
    string? FileName,
    string? FileUrl,
    long? FileSize);

public record SendMessageResponseDto(
    ChatMessageDto UserMessage,
    ChatMessageDto AiMessage,
    bool IsAiFallback = false,
    string? AiErrorType = null);

public record ChatMessageListDto(
    List<ChatMessageDto> Messages,
    int Total,
    int Page,
    int PageSize,
    DateTime? Before,
    DateTime? After);

public record ChatStatisticsDto(
    int TotalSessions,
    int TotalMessages,
    long TotalTokens,
    List<PersonaChatStatsDto> PersonaStats,
    int MessagesLast7Days,
    int MessagesLast30Days,
    List<DailyActivityDto> DailyActivity);

public record PersonaChatStatsDto(
    Guid PersonaId,
    string PersonaName,
    int SessionCount,
    int MessageCount);

public record DailyActivityDto(
    DateTime Date,
    int MessageCount);

public record ExportResultDto(
    string Format,
    string Content,
    string? FileName);
