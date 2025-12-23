using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Personas.Queries.SearchPersonas;

public record SearchPersonasQuery(string Query, int Page = 1, int PageSize = 20)
    : IRequest<Result<PersonaListDto>>;

public class SearchPersonasQueryHandler : IRequestHandler<SearchPersonasQuery, Result<PersonaListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SearchPersonasQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PersonaListDto>> Handle(SearchPersonasQuery request, CancellationToken cancellationToken)
    {
        var searchTerm = request.Query.ToLower();

        var query = _context.Personas
            .Include(p => p.Creator)
            .Where(p => p.IsPublic && p.Status == PersonaStatus.Active)
            .Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.Bio != null && p.Bio.ToLower().Contains(searchTerm)) ||
                (p.Tags != null && p.Tags.Any(t => t.ToLower().Contains(searchTerm))))
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var personas = await query
            .OrderByDescending(p => p.LikeCount)
            .ThenByDescending(p => p.ViewCount)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var personaIds = personas.Select(p => p.Id).ToList();
        var likedIds = new HashSet<Guid>();

        if (_currentUser.UserId.HasValue)
        {
            likedIds = (await _context.PersonaLikes
                .Where(l => l.UserId == _currentUser.UserId && personaIds.Contains(l.PersonaId))
                .Select(l => l.PersonaId)
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        var dtos = personas.Select(p => new PersonaDto(
            p.Id, p.CreatorId, p.Name, p.Description, p.Bio, p.ImagePath,
            p.PersonalityTraits, p.LanguageStyle, p.Expertise, p.Tags,
            p.VoiceId, p.VoiceSettings, p.IsPublic, p.IsMarketplace,
            p.Status.ToString(), p.LikeCount, p.ViewCount, p.CloneCount,
            p.CreatedAt, p.UpdatedAt,
            _currentUser.UserId.HasValue && p.CreatorId == _currentUser.UserId,
            likedIds.Contains(p.Id), p.Creator?.DisplayName)).ToList();

        return Result<PersonaListDto>.Success(new PersonaListDto(dtos, total, request.Page, request.PageSize));
    }
}
