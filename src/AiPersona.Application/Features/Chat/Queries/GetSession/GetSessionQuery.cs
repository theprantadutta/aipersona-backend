using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;

namespace AiPersona.Application.Features.Chat.Queries.GetSession;

public record GetSessionQuery(Guid SessionId, bool IncludeMessages = true, int MessagesLimit = 100)
    : IRequest<Result<ChatSessionDetailDto>>;

public class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, Result<ChatSessionDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSessionQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ChatSessionDetailDto>> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatSessionDetailDto>.Failure("Unauthorized", 401);

        var session = await _context.ChatSessions
            .Include(s => s.Persona)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == _currentUser.UserId, cancellationToken);

        if (session == null)
            return Result<ChatSessionDetailDto>.Failure("Session not found", 404);

        var messages = new List<ChatMessageDto>();
        if (request.IncludeMessages)
        {
            var msgs = await _context.ChatMessages
                .Where(m => m.SessionId == session.Id)
                .OrderBy(m => m.CreatedAt)
                .Take(request.MessagesLimit)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            messages = msgs.Select(m => new ChatMessageDto(
                m.Id, m.SessionId, m.SenderId, m.SenderType.ToString(), m.Text,
                m.MessageType.ToString(), m.TokensUsed, m.CreatedAt,
                ParseMetadata(m.MetaData))).ToList();
        }

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

        return Result<ChatSessionDetailDto>.Success(new ChatSessionDetailDto(
            session.Id, session.UserId, session.PersonaId, session.PersonaName,
            session.Persona?.ImagePath, title, session.Status.ToString(), session.IsPinned,
            session.MessageCount, session.CreatedAt, session.LastMessageAt, session.UpdatedAt, messages));
    }

    private static Dictionary<string, object>? ParseMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }
}
