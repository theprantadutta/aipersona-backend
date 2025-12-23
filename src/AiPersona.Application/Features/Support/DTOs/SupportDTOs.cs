namespace AiPersona.Application.Features.Support.DTOs;

public record SupportTicketDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string? UserName,
    string Subject,
    string Category,
    string Priority,
    string Status,
    Guid? AssignedToId,
    string? AssignedToName,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt);

public record SupportTicketListDto(
    List<SupportTicketDto> Tickets,
    int Total,
    int Page,
    int PageSize);

public record SupportTicketDetailDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string? UserName,
    string Subject,
    string Category,
    string Priority,
    string Status,
    Guid? AssignedToId,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    List<SupportMessageDto> Messages);

public record SupportMessageDto(
    Guid Id,
    Guid TicketId,
    Guid SenderId,
    string SenderName,
    bool IsStaffReply,
    string Content,
    string? Attachments,
    DateTime CreatedAt);

public record SupportMessageListDto(
    List<SupportMessageDto> Messages,
    int Total,
    int Page,
    int PageSize);

public record CreateTicketResultDto(
    Guid TicketId,
    string Status,
    string Message);

public record AddMessageResultDto(
    Guid MessageId,
    string Message);

public record TicketActionResultDto(
    bool Success,
    string Status,
    string Message);
