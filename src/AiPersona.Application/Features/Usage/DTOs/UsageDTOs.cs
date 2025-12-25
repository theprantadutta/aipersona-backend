namespace AiPersona.Application.Features.Usage.DTOs;

public record CurrentUsageDto(
    int MessagesToday,
    int MessagesLimit,
    int PersonasCount,        // Changed from PersonaCount to match Flutter
    int PersonasLimit,
    long StorageUsedBytes,
    double StorageUsedMb,     // NEW - for Flutter compatibility
    long StorageLimitBytes,
    int ConversationHistoryDays,
    int HistoryDaysLimit,
    string Tier,
    bool IsPremium,           // NEW - derived from tier
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
    double DailyAverage,          // renamed from AvgMessagesPerDay
    double AvgTokensPerMessage,
    int MostActiveHour,
    string PeakUsageDay,          // renamed from MostActiveDayOfWeek
    int PeakUsageCount,           // NEW - max messages in a single day
    string Trend,                 // NEW - "increasing", "decreasing", "stable"
    double? UsagePercentage,      // NEW
    Dictionary<string, int> MessagesByPersona,
    List<DailyUsageDto> DailyUsage,  // renamed from Last30Days
    Dictionary<string, object>? Predictions);  // NEW

public record DailyUsageDto(
    DateTime Date,
    int Messages,
    int Tokens,
    int Sessions);
