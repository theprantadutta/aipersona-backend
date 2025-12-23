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

        var personaStats = sessions
            .Where(s => s.PersonaId.HasValue)
            .GroupBy(s => new { PersonaId = s.PersonaId!.Value, s.PersonaName })
            .Select(g => new PersonaChatStatsDto(
                g.Key.PersonaId,
                g.Key.PersonaName,
                g.Count(),
                g.Sum(s => s.MessageCount)))
            .OrderByDescending(p => p.MessageCount)
            .ToList();

        var today = DateTime.UtcNow.Date;
        var last7Days = today.AddDays(-7);
        var last30Days = today.AddDays(-30);

        var messagesLast7Days = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && m.CreatedAt >= last7Days)
            .CountAsync(cancellationToken);

        var messagesLast30Days = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && m.CreatedAt >= last30Days)
            .CountAsync(cancellationToken);

        var dailyActivity = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && m.CreatedAt >= last30Days)
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new DailyActivityDto(g.Key, g.Count()))
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);

        return Result<ChatStatisticsDto>.Success(new ChatStatisticsDto(
            sessions.Count,
            totalMessages,
            totalTokens,
            personaStats,
            messagesLast7Days,
            messagesLast30Days,
            dailyActivity));
    }
}
