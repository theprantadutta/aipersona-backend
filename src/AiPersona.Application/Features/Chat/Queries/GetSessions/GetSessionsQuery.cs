using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Chat.Queries.GetSessions;

public record GetSessionsQuery(string? Status = null, int Page = 1, int PageSize = 50)
    : IRequest<Result<ChatSessionListDto>>;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, Result<ChatSessionListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSessionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ChatSessionListDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatSessionListDto>.Failure("Unauthorized", 401);

        var query = _context.ChatSessions
            .Include(s => s.Persona)
            .Where(s => s.UserId == _currentUser.UserId)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ChatSessionStatus>(request.Status, true, out var status))
        {
            query = query.Where(s => s.Status == status);
        }
        else
        {
            query = query.Where(s => s.Status != ChatSessionStatus.Deleted);
        }

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var sessions = await query
            .OrderByDescending(s => s.IsPinned)
            .ThenByDescending(s => s.LastMessageAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get last messages
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var lastMessages = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId))
            .GroupBy(m => m.SessionId)
            .Select(g => new { SessionId = g.Key, LastMessage = g.OrderByDescending(m => m.CreatedAt).First() })
            .ToDictionaryAsync(x => x.SessionId, x => x.LastMessage.Text, cancellationToken);

        var dtos = sessions.Select(s =>
        {
            string? title = null;
            if (!string.IsNullOrEmpty(s.MetaData))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(s.MetaData);
                    if (metadata?.TryGetValue("title", out var t) == true)
                        title = t?.ToString();
                }
                catch { }
            }

            var lastMsg = lastMessages.TryGetValue(s.Id, out var msg) ? msg : null;
            var isDeleted = s.PersonaDeletedAt != null || s.Persona?.Status == PersonaStatus.Archived;

            // Parse settings and tags from metadata
            Dictionary<string, object>? settings = null;
            List<string>? tags = null;
            if (!string.IsNullOrEmpty(s.MetaData))
            {
                try
                {
                    var md = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(s.MetaData);
                    if (md != null)
                    {
                        settings = md.Where(kv => kv.Key != "tags" && kv.Key != "title")
                            .ToDictionary(kv => kv.Key, kv => (object)kv.Value);
                        if (md.TryGetValue("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                            tags = tagsEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                    }
                }
                catch { }
            }

            return new ChatSessionDto(
                s.Id, s.UserId, s.PersonaId, s.PersonaName,
                isDeleted ? s.DeletedPersonaImage : s.Persona?.ImagePath,
                title, s.Status.ToString(), s.IsPinned, s.MessageCount,
                lastMsg?.Substring(0, Math.Min(lastMsg.Length, 200)),
                s.CreatedAt, s.LastMessageAt, s.UpdatedAt, isDeleted,
                s.DeletedPersonaName, s.DeletedPersonaImage, s.PersonaDeletedAt,
                settings, tags);
        }).ToList();

        return Result<ChatSessionListDto>.Success(new ChatSessionListDto(dtos, total, request.Page, request.PageSize));
    }
}
