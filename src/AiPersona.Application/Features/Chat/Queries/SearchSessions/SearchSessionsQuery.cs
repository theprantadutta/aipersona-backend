using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Chat.Queries.SearchSessions;

public record SearchSessionsQuery(
    string? Query = null,
    Guid? PersonaId = null,
    string? Status = null,
    bool? IsPinned = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string SortBy = "last_message_at",
    string SortOrder = "desc",
    int Page = 1,
    int PageSize = 20) : IRequest<Result<ChatSessionSearchDto>>;

public class SearchSessionsQueryHandler : IRequestHandler<SearchSessionsQuery, Result<ChatSessionSearchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SearchSessionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ChatSessionSearchDto>> Handle(SearchSessionsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatSessionSearchDto>.Failure("Unauthorized", 401);

        var query = _context.ChatSessions
            .Include(s => s.Persona)
            .Where(s => s.UserId == _currentUser.UserId && s.Status != ChatSessionStatus.Deleted)
            .AsNoTracking();

        var filtersApplied = new List<string>();

        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerm = request.Query.ToLower();
            query = query.Where(s => s.PersonaName.ToLower().Contains(searchTerm));
            filtersApplied.Add("query");
        }

        if (request.PersonaId.HasValue)
        {
            query = query.Where(s => s.PersonaId == request.PersonaId);
            filtersApplied.Add("persona");
        }

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ChatSessionStatus>(request.Status, true, out var status))
        {
            query = query.Where(s => s.Status == status);
            filtersApplied.Add("status");
        }

        if (request.IsPinned.HasValue)
        {
            query = query.Where(s => s.IsPinned == request.IsPinned);
            filtersApplied.Add("pinned");
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= request.StartDate);
            filtersApplied.Add("start_date");
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= request.EndDate);
            filtersApplied.Add("end_date");
        }

        // Apply sorting
        query = (request.SortBy.ToLower(), request.SortOrder.ToLower()) switch
        {
            ("created_at", "asc") => query.OrderBy(s => s.IsPinned).ThenBy(s => s.CreatedAt),
            ("created_at", _) => query.OrderByDescending(s => s.IsPinned).ThenByDescending(s => s.CreatedAt),
            ("message_count", "asc") => query.OrderBy(s => s.IsPinned).ThenBy(s => s.MessageCount),
            ("message_count", _) => query.OrderByDescending(s => s.IsPinned).ThenByDescending(s => s.MessageCount),
            ("persona_name", "asc") => query.OrderBy(s => s.IsPinned).ThenBy(s => s.PersonaName),
            ("persona_name", _) => query.OrderByDescending(s => s.IsPinned).ThenByDescending(s => s.PersonaName),
            (_, "asc") => query.OrderBy(s => s.IsPinned).ThenBy(s => s.LastMessageAt),
            _ => query.OrderByDescending(s => s.IsPinned).ThenByDescending(s => s.LastMessageAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var sessions = await query.Skip(skip).Take(request.PageSize).ToListAsync(cancellationToken);

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

            return new ChatSessionDto(
                s.Id, s.UserId, s.PersonaId, s.PersonaName, s.Persona?.ImagePath,
                title, s.Status.ToString(), s.IsPinned, s.MessageCount, null,
                s.CreatedAt, s.LastMessageAt, s.UpdatedAt);
        }).ToList();

        return Result<ChatSessionSearchDto>.Success(new ChatSessionSearchDto(
            dtos, total, request.Page, request.PageSize, request.Query, filtersApplied));
    }
}
