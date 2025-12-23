using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Chat.Commands.UpdateSession;

public record UpdateSessionCommand(
    Guid SessionId,
    string? Title = null,
    bool? IsPinned = null,
    string? Status = null) : IRequest<Result<ChatSessionDto>>;

public class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand, Result<ChatSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public UpdateSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ChatSessionDto>> Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatSessionDto>.Failure("Unauthorized", 401);

        var session = await _context.ChatSessions
            .Include(s => s.Persona)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == _currentUser.UserId, cancellationToken);

        if (session == null)
            return Result<ChatSessionDto>.Failure("Session not found", 404);

        // Parse existing metadata or create new
        var metadata = string.IsNullOrEmpty(session.MetaData)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(session.MetaData) ?? new Dictionary<string, object>();

        if (request.Title != null)
            metadata["title"] = request.Title;

        session.MetaData = JsonSerializer.Serialize(metadata);

        if (request.IsPinned.HasValue)
            session.IsPinned = request.IsPinned.Value;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ChatSessionStatus>(request.Status, true, out var status))
            session.Status = status;

        session.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        string? title = null;
        Dictionary<string, object>? settings = null;
        List<string>? tags = null;

        // Re-parse metadata for response
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
