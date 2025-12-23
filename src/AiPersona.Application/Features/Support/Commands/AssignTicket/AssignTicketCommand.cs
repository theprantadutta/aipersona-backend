using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Support.Commands.AssignTicket;

public record AssignTicketCommand(Guid TicketId, Guid? AssigneeId = null) : IRequest<Result<TicketActionResultDto>>;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result<TicketActionResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public AssignTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<TicketActionResultDto>> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<TicketActionResultDto>.Failure("Admin access required", 403);

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<TicketActionResultDto>.Failure("Ticket not found", 404);

        var assigneeId = request.AssigneeId ?? _currentUser.UserId.Value;

        if (assigneeId != _currentUser.UserId)
        {
            var assignee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == assigneeId && u.IsAdmin, cancellationToken);

            if (assignee == null)
                return Result<TicketActionResultDto>.Failure("Assignee not found or not an admin", 400);
        }

        ticket.AssignedToId = assigneeId;
        ticket.Status = TicketStatus.InProgress;
        ticket.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TicketActionResultDto>.Success(new TicketActionResultDto(
            true, ticket.Status.ToString(), "Ticket assigned"));
    }
}
