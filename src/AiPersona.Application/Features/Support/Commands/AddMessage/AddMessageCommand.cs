using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;
using AiPersona.Domain.Entities;
using System.Text.Json;

namespace AiPersona.Application.Features.Support.Commands.AddMessage;

public record AddMessageCommand(
    Guid TicketId,
    string Content,
    List<string>? Attachments = null) : IRequest<Result<AddMessageResultDto>>;

public class AddMessageCommandHandler : IRequestHandler<AddMessageCommand, Result<AddMessageResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public AddMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<AddMessageResultDto>> Handle(AddMessageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<AddMessageResultDto>.Failure("Unauthorized", 401);

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<AddMessageResultDto>.Failure("Ticket not found", 404);

        // Non-admin users can only message their own tickets
        if (!_currentUser.IsAdmin && ticket.UserId != _currentUser.UserId)
            return Result<AddMessageResultDto>.Failure("Access denied", 403);

        var message = new SupportTicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            SenderId = _currentUser.UserId.Value,
            IsStaffReply = _currentUser.IsAdmin,
            Content = request.Content,
            Attachments = request.Attachments != null && request.Attachments.Count > 0
                ? JsonSerializer.Serialize(request.Attachments)
                : null,
            CreatedAt = _dateTime.UtcNow
        };

        _context.SupportTicketMessages.Add(message);
        ticket.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<AddMessageResultDto>.Success(new AddMessageResultDto(
            message.Id, "Message added"));
    }
}
