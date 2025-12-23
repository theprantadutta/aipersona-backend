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
        Dictionary<string, object>? settings = null;
        List<string>? tags = null;
        if (!string.IsNullOrEmpty(session.MetaData))
        {
            try
            {
                var md = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(session.MetaData);
                if (md != null)
                {
                    if (md.TryGetValue("title", out var titleEl))
                        title = titleEl.GetString();
                    settings = md.Where(kv => kv.Key != "tags" && kv.Key != "title")
                        .ToDictionary(kv => kv.Key, kv => (object)kv.Value);
                    if (md.TryGetValue("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                        tags = tagsEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                }
            }
            catch { }
        }

        var isDeleted = session.PersonaDeletedAt != null;

        return Result<ChatSessionDto>.Success(new ChatSessionDto(
            session.Id, session.UserId, session.PersonaId, session.PersonaName,
            isDeleted ? session.DeletedPersonaImage : session.Persona?.ImagePath,
            title, session.Status.ToString(), session.IsPinned, session.MessageCount, null,
            session.CreatedAt, session.LastMessageAt, session.UpdatedAt, isDeleted,
            session.DeletedPersonaName, session.DeletedPersonaImage, session.PersonaDeletedAt,
            settings, tags));
    }
}
