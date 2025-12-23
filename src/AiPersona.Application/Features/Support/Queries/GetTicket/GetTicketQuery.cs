using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;

namespace AiPersona.Application.Features.Support.Queries.GetTicket;

public record GetTicketQuery(Guid TicketId) : IRequest<Result<SupportTicketDetailDto>>;

public class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, Result<SupportTicketDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTicketQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportTicketDetailDto>> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SupportTicketDetailDto>.Failure("Unauthorized", 401);

        var ticket = await _context.SupportTickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<SupportTicketDetailDto>.Failure("Ticket not found", 404);

        if (!_currentUser.IsAdmin && ticket.UserId != _currentUser.UserId)
            return Result<SupportTicketDetailDto>.Failure("Access denied", 403);

        var messages = await _context.SupportTicketMessages
            .Include(m => m.Sender)
            .Where(m => m.TicketId == ticket.Id)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var messageDtos = messages.Select(m => new SupportMessageDto(
            m.Id,
            m.TicketId,
            m.SenderId,
            m.Sender?.DisplayName ?? m.Sender?.Email ?? "Unknown",
            m.IsStaffReply,
            m.Content,
            m.Attachments,
            m.CreatedAt)).ToList();

        return Result<SupportTicketDetailDto>.Success(new SupportTicketDetailDto(
            ticket.Id,
            ticket.UserId,
            ticket.User?.Email ?? "Unknown",
            ticket.User?.DisplayName,
            ticket.Subject,
            ticket.Category.ToString(),
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.AssignedToId,
            ticket.AssignedTo?.DisplayName ?? ticket.AssignedTo?.Email,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            messageDtos));
    }
}
