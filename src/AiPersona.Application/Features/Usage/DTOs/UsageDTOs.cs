namespace AiPersona.Application.Features.Usage.DTOs;

public record CurrentUsageDto(
    int MessagesToday,
    int MessagesLimit,  // Changed from MessageLimit to match Flutter
    int PersonaCount,
    int PersonasLimit,  // Changed from PersonaLimit to match Flutter
    long StorageUsedBytes,
    long StorageLimitBytes,
    int ConversationHistoryDays,
    int HistoryDaysLimit,
    string Tier,
    DateTime? ResetAt);

public record UsageHistoryItemDto(
    DateTime Date,
    int MessageCount,
    int TokensUsed,
    int SessionsCreated,
    long StorageUsedBytes);

public record UsageHistoryDto(
    List<UsageHistoryItemDto> History,
    int Total,
    int Page,
    int PageSize);

public record UsageAnalyticsDto(
    int TotalMessages,
    int TotalTokens,
    int TotalSessions,
    double AvgMessagesPerDay,
    double AvgTokensPerMessage,
    int MostActiveHour,
    string MostActiveDayOfWeek,
    Dictionary<string, int> MessagesByPersona,
    List<DailyUsageDto> Last30Days);

public record DailyUsageDto(
    DateTime Date,
    int Messages,
    int Tokens,
    int Sessions);
