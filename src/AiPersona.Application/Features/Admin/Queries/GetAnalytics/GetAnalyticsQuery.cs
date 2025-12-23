using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;

namespace AiPersona.Application.Features.Admin.Queries.GetAnalytics;

public record GetAnalyticsQuery(int Days = 30) : IRequest<Result<AdminAnalyticsDto>>;

public class GetAnalyticsQueryHandler : IRequestHandler<GetAnalyticsQuery, Result<AdminAnalyticsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetAnalyticsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<AdminAnalyticsDto>> Handle(GetAnalyticsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<AdminAnalyticsDto>.Failure("Admin access required", 403);

        var today = _dateTime.UtcNow.Date;
        var startDate = today.AddDays(-request.Days);
        var weekAgo = today.AddDays(-7);

        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var activeUsers = await _context.Users.Where(u => u.LastActiveAt >= startDate).CountAsync(cancellationToken);
        var newUsersToday = await _context.Users.Where(u => u.CreatedAt >= today).CountAsync(cancellationToken);
        var newUsersThisWeek = await _context.Users.Where(u => u.CreatedAt >= weekAgo).CountAsync(cancellationToken);
        var totalPersonas = await _context.Personas.CountAsync(cancellationToken);
        var totalMessages = await _context.ChatMessages.CountAsync(cancellationToken);
        var messagesToday = await _context.ChatMessages.Where(m => m.CreatedAt >= today).CountAsync(cancellationToken);

        var usersByTier = await _context.Users
            .GroupBy(u => u.SubscriptionTier)
            .Select(g => new { Tier = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Tier, x => x.Count, cancellationToken);

        var dailyMetrics = new List<DailyMetricDto>();
        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            var nextDate = date.AddDays(1);
            var newUsers = await _context.Users.Where(u => u.CreatedAt >= date && u.CreatedAt < nextDate).CountAsync(cancellationToken);
            var activeUsersDay = await _context.Users.Where(u => u.LastActiveAt >= date && u.LastActiveAt < nextDate).CountAsync(cancellationToken);
            var messages = await _context.ChatMessages.Where(m => m.CreatedAt >= date && m.CreatedAt < nextDate).CountAsync(cancellationToken);
            var newPersonas = await _context.Personas.Where(p => p.CreatedAt >= date && p.CreatedAt < nextDate).CountAsync(cancellationToken);

            dailyMetrics.Add(new DailyMetricDto(date, newUsers, activeUsersDay, messages, newPersonas));
        }

        return Result<AdminAnalyticsDto>.Success(new AdminAnalyticsDto(
            totalUsers,
            activeUsers,
            newUsersToday,
            newUsersThisWeek,
            totalPersonas,
            totalMessages,
            messagesToday,
            usersByTier,
            new Dictionary<string, int>(),
            dailyMetrics));
    }
}
