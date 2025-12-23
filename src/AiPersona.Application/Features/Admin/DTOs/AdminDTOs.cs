namespace AiPersona.Application.Features.Admin.DTOs;

public record AdminUserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? ProfileImage,
    string SubscriptionTier,
    bool IsAdmin,
    bool IsSuspended,
    DateTime? SuspendedUntil,
    int PersonaCount,
    int MessageCount,
    DateTime CreatedAt,
    DateTime? LastActiveAt);

public record AdminUserListDto(
    List<AdminUserDto> Users,
    int Total,
    int Page,
    int PageSize);

public record AdminReportDto(
    Guid Id,
    Guid ReporterId,
    string ReporterName,
    string ContentType,
    Guid ContentId,
    string Reason,
    string? Description,
    string Status,
    Guid? ResolvedById,
    string? ResolvedByName,
    string? Resolution,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public record AdminReportListDto(
    List<AdminReportDto> Reports,
    int Total,
    int Page,
    int PageSize);

public record AdminAnalyticsDto(
    int TotalUsers,
    int ActiveUsers,
    int NewUsersToday,
    int NewUsersThisWeek,
    int TotalPersonas,
    int TotalMessages,
    int MessagesToday,
    Dictionary<string, int> UsersByTier,
    Dictionary<string, int> UsersByPlatform,
    List<DailyMetricDto> DailyMetrics);

public record DailyMetricDto(
    DateTime Date,
    int NewUsers,
    int ActiveUsers,
    int Messages,
    int NewPersonas);

public record AdminDashboardDto(
    int TotalUsers,
    int ActiveUsersToday,
    int TotalPersonas,
    int TotalMessages,
    int PendingReports,
    int OpenTickets,
    double RevenueThisMonth,
    List<AdminReportDto> RecentReports,
    List<AdminUserDto> NewestUsers);

public record UpdateUserResultDto(bool Success, string Message);
public record SuspendResultDto(bool Success, string Message, DateTime? SuspendedUntil);
public record ResolveReportResultDto(bool Success, string Message);

public record UserDetailDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? ProfileImage,
    string AuthProvider,
    string SubscriptionTier,
    bool IsActive,
    bool IsSuspended,
    string? SuspensionReason,
    DateTime? SuspendedUntil,
    int PersonaCount,
    int SessionCount,
    int MessageCount,
    long StorageUsedBytes,
    long TokensUsedTotal,
    DateTime CreatedAt,
    DateTime? LastLogin);
