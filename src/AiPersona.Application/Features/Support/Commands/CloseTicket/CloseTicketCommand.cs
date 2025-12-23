using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Support.Commands.CloseTicket;

public record CloseTicketCommand(Guid TicketId, string? Resolution = null) : IRequest<Result<TicketActionResultDto>>;

public class CloseTicketCommandHandler : IRequestHandler<CloseTicketCommand, Result<TicketActionResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public CloseTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<TicketActionResultDto>> Handle(CloseTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<TicketActionResultDto>.Failure("Unauthorized", 401);

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<TicketActionResultDto>.Failure("Ticket not found", 404);

        if (!_currentUser.IsAdmin && ticket.UserId != _currentUser.UserId)
            return Result<TicketActionResultDto>.Failure("Access denied", 403);

        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedAt = _dateTime.UtcNow;
        ticket.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TicketActionResultDto>.Success(new TicketActionResultDto(
            true, ticket.Status.ToString(), "Ticket closed"));
    }
}
