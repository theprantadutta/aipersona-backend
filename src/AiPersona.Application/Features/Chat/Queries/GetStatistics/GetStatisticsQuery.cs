using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Chat.Queries.GetStatistics;

public record GetStatisticsQuery : IRequest<Result<ChatStatisticsDto>>;

public class GetStatisticsQueryHandler : IRequestHandler<GetStatisticsQuery, Result<ChatStatisticsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetStatisticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ChatStatisticsDto>> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatStatisticsDto>.Failure("Unauthorized", 401);

        var userId = _currentUser.UserId.Value;

        var sessions = await _context.ChatSessions
            .Include(s => s.Persona)
            .Where(s => s.UserId == userId && s.Status != ChatSessionStatus.Deleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var totalMessages = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId))
            .CountAsync(cancellationToken);

        var totalTokens = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId))
            .SumAsync(m => m.TokensUsed, cancellationToken);

        // Session counts by status
        var activeSessions = sessions.Count(s => s.Status == ChatSessionStatus.Active);
        var archivedSessions = sessions.Count(s => s.Status == ChatSessionStatus.Archived);
        var pinnedSessions = sessions.Count(s => s.IsPinned);
        var uniquePersonas = sessions.Where(s => s.PersonaId.HasValue).Select(s => s.PersonaId).Distinct().Count();

        // Persona stats with image URL
        var personasActivity = sessions
            .Where(s => s.PersonaId.HasValue)
            .GroupBy(s => new { PersonaId = s.PersonaId!.Value, s.PersonaName, ImageUrl = s.Persona?.ImagePath })
            .Select(g => new PersonaChatStatsDto(
                g.Key.PersonaId,
                g.Key.PersonaName,
                g.Key.ImageUrl,
                g.Count(),
                g.Sum(s => s.MessageCount)))
            .OrderByDescending(p => p.MessageCount)
            .ToList();

        // Most active persona
        var mostActivePersona = personasActivity.FirstOrDefault();

        // Average messages per session
        var avgMessagesPerSession = sessions.Count > 0 ? (double)totalMessages / sessions.Count : 0;

        var today = DateTime.UtcNow.Date;
        var last7Days = today.AddDays(-7);
        var last30Days = today.AddDays(-30);

        var messagesLast7Days = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && m.CreatedAt >= last7Days)
            .CountAsync(cancellationToken);

        var messagesLast30Days = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && m.CreatedAt >= last30Days)
            .CountAsync(cancellationToken);

        // Fetch messages and sessions created dates for daily activity
        var messagesDates = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && m.CreatedAt >= last7Days)
            .Select(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var sessionsCreatedDates = sessions
            .Where(s => s.CreatedAt >= last7Days)
            .Select(s => s.CreatedAt.Date)
            .ToList();

        // Group by date with sessions created count
        var weeklyActivity = messagesDates
            .GroupBy(d => d.Date)
            .Select(g => new DailyActivityDto(
                g.Key,
                sessionsCreatedDates.Count(d => d == g.Key),
                g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        // Most active day of week
        var mostActiveDay = messagesDates.Count > 0
            ? messagesDates.GroupBy(d => d.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .First().Key.ToString()
            : null;

        return Result<ChatStatisticsDto>.Success(new ChatStatisticsDto(
            sessions.Count,
            totalMessages,
            totalTokens,
            activeSessions,
            archivedSessions,
            pinnedSessions,
            uniquePersonas,
            mostActivePersona,
            personasActivity,
            weeklyActivity,
            avgMessagesPerSession,
            mostActiveDay,
            messagesLast7Days,
            messagesLast30Days));
    }
}
