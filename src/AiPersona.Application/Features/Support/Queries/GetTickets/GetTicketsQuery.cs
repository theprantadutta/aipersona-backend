using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Support.Queries.GetTickets;

public record GetTicketsQuery(
    string? Status = null,
    string? Priority = null,
    bool MyTicketsOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<SupportTicketListDto>>;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, Result<SupportTicketListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTicketsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportTicketListDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SupportTicketListDto>.Failure("Unauthorized", 401);

        var query = _context.SupportTickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .AsNoTracking();

        if (!_currentUser.IsAdmin || request.MyTicketsOnly)
            query = query.Where(t => t.UserId == _currentUser.UserId);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TicketStatus>(request.Status, true, out var status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<TicketPriority>(request.Priority, true, out var priority))
            query = query.Where(t => t.Priority == priority);

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var tickets = await query
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.UpdatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var ticketIds = tickets.Select(t => t.Id).ToList();
        var messageCounts = await _context.SupportTicketMessages
            .Where(m => ticketIds.Contains(m.TicketId))
            .GroupBy(m => m.TicketId)
            .Select(g => new { TicketId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TicketId, x => x.Count, cancellationToken);

        var dtos = tickets.Select(t => new SupportTicketDto(
            t.Id,
            t.UserId,
            t.User?.Email ?? "Unknown",
            t.User?.DisplayName,
            t.Subject,
            t.Category.ToString(),
            t.Priority.ToString(),
            t.Status.ToString(),
            t.AssignedToId,
            t.AssignedTo?.DisplayName ?? t.AssignedTo?.Email,
            messageCounts.TryGetValue(t.Id, out var count) ? count : 0,
            t.CreatedAt,
            t.UpdatedAt,
            t.ResolvedAt)).ToList();

        return Result<SupportTicketListDto>.Success(new SupportTicketListDto(dtos, total, request.Page, request.PageSize));
    }
}
