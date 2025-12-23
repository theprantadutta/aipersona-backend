using MediatR;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Support.Commands.CreateTicket;

public record CreateTicketCommand(
    string Subject,
    string Category,
    string? Priority,
    string InitialMessage) : IRequest<Result<CreateTicketResultDto>>;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<CreateTicketResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public CreateTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<CreateTicketResultDto>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<CreateTicketResultDto>.Failure("Unauthorized", 401);

        if (!Enum.TryParse<TicketCategory>(request.Category, true, out var category))
            return Result<CreateTicketResultDto>.Failure("Invalid category", 400);

        var priority = TicketPriority.Medium;
        if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<TicketPriority>(request.Priority, true, out var p))
            priority = p;

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            Subject = request.Subject,
            Category = category,
            Priority = priority,
            Status = TicketStatus.Open,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };

        _context.SupportTickets.Add(ticket);

        var message = new SupportTicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            SenderId = _currentUser.UserId.Value,
            IsStaffReply = false,
            Content = request.InitialMessage,
            CreatedAt = _dateTime.UtcNow
        };

        _context.SupportTicketMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateTicketResultDto>.Success(new CreateTicketResultDto(
            ticket.Id, ticket.Status.ToString(), "Ticket created successfully"));
    }
}
