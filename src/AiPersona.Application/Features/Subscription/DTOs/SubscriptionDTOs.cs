namespace AiPersona.Application.Features.Subscription.DTOs;

public record SubscriptionStatusDto(
    string Tier,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime? StartedAt,
    bool AutoRenew,
    string? ProductId,
    string? Platform);

public record SubscriptionEventDto(
    Guid Id,
    string EventType,
    string Tier,
    string? ProductId,
    string? Platform,
    string? TransactionId,
    DateTime CreatedAt);

public record SubscriptionHistoryDto(
    List<SubscriptionEventDto> Events,
    int Total,
    int Page,
    int PageSize);

public record SubscriptionPlanDto(
    string Id,
    string Name,
    string Tier,
    decimal MonthlyPrice,
    decimal? YearlyPrice,
    string Currency,
    List<string> Features,
    int MessageLimit,
    int PersonaLimit,
    int StorageMb,
    int HistoryDays);

public record SubscriptionPlansDto(
    List<SubscriptionPlanDto> Plans);

public record VerifyPurchaseResultDto(
    bool IsValid,
    string Tier,
    DateTime? ExpiresAt,
    string Message);

public record CancelSubscriptionResultDto(
    bool Success,
    string Message,
    DateTime? ActiveUntil);
