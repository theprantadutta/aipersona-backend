using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Support.Commands.UpdateTicket;

public record UpdateTicketCommand(
    Guid TicketId,
    string? Priority = null,
    string? Status = null,
    string? Category = null) : IRequest<Result<TicketActionResultDto>>;

public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand, Result<TicketActionResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public UpdateTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<TicketActionResultDto>> Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<TicketActionResultDto>.Failure("Admin access required", 403);

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<TicketActionResultDto>.Failure("Ticket not found", 404);

        if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<TicketPriority>(request.Priority, true, out var priority))
            ticket.Priority = priority;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TicketStatus>(request.Status, true, out var status))
            ticket.Status = status;

        if (!string.IsNullOrEmpty(request.Category) && Enum.TryParse<TicketCategory>(request.Category, true, out var category))
            ticket.Category = category;

        ticket.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TicketActionResultDto>.Success(new TicketActionResultDto(
            true, ticket.Status.ToString(), "Ticket updated"));
    }
}
