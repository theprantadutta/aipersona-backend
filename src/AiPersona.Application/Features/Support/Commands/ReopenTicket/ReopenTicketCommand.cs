using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Support.Commands.ReopenTicket;

public record ReopenTicketCommand(Guid TicketId, string? Reason = null) : IRequest<Result<TicketActionResultDto>>;

public class ReopenTicketCommandHandler : IRequestHandler<ReopenTicketCommand, Result<TicketActionResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ReopenTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<TicketActionResultDto>> Handle(ReopenTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<TicketActionResultDto>.Failure("Unauthorized", 401);

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<TicketActionResultDto>.Failure("Ticket not found", 404);

        if (!_currentUser.IsAdmin && ticket.UserId != _currentUser.UserId)
            return Result<TicketActionResultDto>.Failure("Access denied", 403);

        if (ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed)
            return Result<TicketActionResultDto>.Failure("Ticket is not closed", 400);

        ticket.Status = TicketStatus.Open;
        ticket.ResolvedAt = null;
        ticket.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TicketActionResultDto>.Success(new TicketActionResultDto(
            true, ticket.Status.ToString(), "Ticket reopened"));
    }
}
