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

        return Result<UsageAnalyticsDto>.Success(new UsageAnalyticsDto(
            totalMessages,
            totalTokens,
            totalSessions,
            avgMessagesPerDay,
            avgTokensPerMessage,
            mostActiveHour,
            mostActiveDayOfWeek,
            messagesByPersona,
            dailyUsage));
    }
}
