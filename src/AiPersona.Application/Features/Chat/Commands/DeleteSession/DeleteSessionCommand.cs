using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Chat.Commands.DeleteSession;

public record DeleteSessionCommand(Guid SessionId) : IRequest<Result>;

public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public DeleteSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result.Failure("Unauthorized", 401);

        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == _currentUser.UserId, cancellationToken);

        if (session == null)
            return Result.Failure("Session not found", 404);

        session.Status = ChatSessionStatus.Deleted;
        session.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
