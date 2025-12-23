using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;

namespace AiPersona.Application.Features.Chat.Commands.TogglePin;

public record TogglePinCommand(Guid SessionId) : IRequest<Result<ChatSessionDto>>;

public class TogglePinCommandHandler : IRequestHandler<TogglePinCommand, Result<ChatSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public TogglePinCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ChatSessionDto>> Handle(TogglePinCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatSessionDto>.Failure("Unauthorized", 401);

        var session = await _context.ChatSessions
            .Include(s => s.Persona)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == _currentUser.UserId, cancellationToken);

        if (session == null)
            return Result<ChatSessionDto>.Failure("Session not found", 404);

        session.IsPinned = !session.IsPinned;
        session.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        string? title = null;
        if (!string.IsNullOrEmpty(session.MetaData))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(session.MetaData);
                if (metadata?.TryGetValue("title", out var t) == true)
                    title = t?.ToString();
            }
            catch { }
        }

        return Result<ChatSessionDto>.Success(new ChatSessionDto(
            session.Id, session.UserId, session.PersonaId, session.PersonaName,
            session.Persona?.ImagePath, title, session.Status.ToString(), session.IsPinned,
            session.MessageCount, null, session.CreatedAt, session.LastMessageAt, session.UpdatedAt));
    }
}
