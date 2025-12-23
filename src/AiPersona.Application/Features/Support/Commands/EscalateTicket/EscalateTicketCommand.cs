using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Support.Commands.EscalateTicket;

public record EscalateTicketCommand(Guid TicketId, string? Reason = null) : IRequest<Result<TicketActionResultDto>>;

public class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand, Result<TicketActionResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public EscalateTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<TicketActionResultDto>> Handle(EscalateTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<TicketActionResultDto>.Failure("Admin access required", 403);

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<TicketActionResultDto>.Failure("Ticket not found", 404);

        // Escalate priority
        ticket.Priority = ticket.Priority switch
        {
            TicketPriority.Low => TicketPriority.Medium,
            TicketPriority.Medium => TicketPriority.High,
            TicketPriority.High => TicketPriority.Urgent,
            _ => ticket.Priority
        };

        ticket.Status = TicketStatus.InProgress;
        ticket.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TicketActionResultDto>.Success(new TicketActionResultDto(
            true, ticket.Status.ToString(), $"Ticket escalated to {ticket.Priority}"));
    }
}
