using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Usage.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Usage.Queries.GetUsageHistory;

public record GetUsageHistoryQuery(int Days = 30, int Page = 1, int PageSize = 30) : IRequest<Result<UsageHistoryDto>>;

public class GetUsageHistoryQueryHandler : IRequestHandler<GetUsageHistoryQuery, Result<UsageHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetUsageHistoryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<UsageHistoryDto>> Handle(GetUsageHistoryQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UsageHistoryDto>.Failure("Unauthorized", 401);

        var startDate = _dateTime.UtcNow.Date.AddDays(-request.Days);

        var sessionIds = await _context.ChatSessions
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var messagesByDate = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && m.CreatedAt >= startDate)
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                Tokens = g.Sum(m => m.TokensUsed)
            })
            .ToListAsync(cancellationToken);

        var sessionsByDate = await _context.ChatSessions
            .Where(s => s.UserId == _currentUser.UserId && s.CreatedAt >= startDate)
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var history = new List<UsageHistoryItemDto>();
        for (var date = startDate; date <= _dateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var messages = messagesByDate.FirstOrDefault(m => m.Date == date);
            var sessions = sessionsByDate.FirstOrDefault(s => s.Date == date);

            history.Add(new UsageHistoryItemDto(
                date,
                messages?.Count ?? 0,
                messages?.Tokens ?? 0,
                sessions?.Count ?? 0,
                0));
        }

        var total = history.Count;
        var skip = (request.Page - 1) * request.PageSize;
        var paged = history.OrderByDescending(h => h.Date).Skip(skip).Take(request.PageSize).ToList();

        return Result<UsageHistoryDto>.Success(new UsageHistoryDto(paged, total, request.Page, request.PageSize));
    }
}
