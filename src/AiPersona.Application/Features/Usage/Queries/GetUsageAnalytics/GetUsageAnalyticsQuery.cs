using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Usage.DTOs;

namespace AiPersona.Application.Features.Usage.Queries.GetUsageAnalytics;

public record GetUsageAnalyticsQuery : IRequest<Result<UsageAnalyticsDto>>;

public class GetUsageAnalyticsQueryHandler : IRequestHandler<GetUsageAnalyticsQuery, Result<UsageAnalyticsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetUsageAnalyticsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<UsageAnalyticsDto>> Handle(GetUsageAnalyticsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UsageAnalyticsDto>.Failure("Unauthorized", 401);

        var sessions = await _context.ChatSessions
            .Where(s => s.UserId == _currentUser.UserId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var messages = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalMessages = messages.Count;
        var totalTokens = messages.Sum(m => m.TokensUsed);
        var totalSessions = sessions.Count;

        var last30Days = _dateTime.UtcNow.Date.AddDays(-30);
        var recentMessages = messages.Where(m => m.CreatedAt >= last30Days).ToList();

        var avgMessagesPerDay = recentMessages.Count / 30.0;
        var avgTokensPerMessage = totalMessages > 0 ? (double)totalTokens / totalMessages : 0;

        var messagesByHour = recentMessages.GroupBy(m => m.CreatedAt.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        var mostActiveHour = messagesByHour?.Key ?? 12;

        var messagesByDay = recentMessages.GroupBy(m => m.CreatedAt.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        var mostActiveDayOfWeek = messagesByDay?.Key.ToString() ?? "Monday";

        var messagesByPersona = sessions
            .GroupBy(s => s.PersonaName)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.MessageCount));

        var dailyUsage = recentMessages
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new DailyUsageDto(
                g.Key,
                g.Count(),
                g.Sum(m => m.TokensUsed),
                sessions.Count(s => s.CreatedAt.Date == g.Key)))
            .OrderBy(d => d.Date)
            .ToList();

        // Calculate peak usage count (max messages in a single day)
        var peakUsageCount = dailyUsage.Count > 0 ? dailyUsage.Max(d => d.Messages) : 0;

        // Calculate trend based on recent vs older activity
        var trend = CalculateTrend(dailyUsage);

        // Calculate usage percentage (for free tier message limits)
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        double? usagePercentage = null;
        if (user?.SubscriptionTier == Domain.Enums.SubscriptionTier.Free)
        {
            var today = _dateTime.UtcNow.Date;
            var todayMessages = recentMessages.Count(m => m.CreatedAt.Date == today);
            usagePercentage = (todayMessages / 50.0) * 100.0; // 50 is free tier limit
        }

        return Result<UsageAnalyticsDto>.Success(new UsageAnalyticsDto(
            totalMessages,
            totalTokens,
            totalSessions,
            avgMessagesPerDay,         // DailyAverage
            avgTokensPerMessage,
            mostActiveHour,
            mostActiveDayOfWeek,       // PeakUsageDay
            peakUsageCount,            // NEW
            trend,                     // NEW
            usagePercentage,           // NEW
            messagesByPersona,
            dailyUsage,                // DailyUsage (renamed from Last30Days)
            null));                    // Predictions (placeholder)
    }

    private static string CalculateTrend(List<DailyUsageDto> dailyUsage)
    {
        if (dailyUsage.Count < 7) return "stable";

        var recentWeek = dailyUsage.TakeLast(7).Average(d => d.Messages);
        var previousWeek = dailyUsage.SkipLast(7).TakeLast(7).ToList();

        if (previousWeek.Count == 0) return "stable";

        var previousAvg = previousWeek.Average(d => d.Messages);

        if (previousAvg == 0) return recentWeek > 0 ? "increasing" : "stable";

        var changePercent = ((recentWeek - previousAvg) / previousAvg) * 100;

        if (changePercent > 10) return "increasing";
        if (changePercent < -10) return "decreasing";
        return "stable";
    }
}
