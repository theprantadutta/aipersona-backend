using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Personas.Queries.GetUserPersonas;

public record GetUserPersonasQuery(string? Status = null, int Page = 1, int PageSize = 50)
    : IRequest<Result<PersonaListDto>>;

public class GetUserPersonasQueryHandler : IRequestHandler<GetUserPersonasQuery, Result<PersonaListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUserPersonasQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PersonaListDto>> Handle(GetUserPersonasQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<PersonaListDto>.Failure("Unauthorized", 401);

        var query = _context.Personas
            .Where(p => p.CreatorId == _currentUser.UserId)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PersonaStatus>(request.Status, true, out var status))
        {
            query = query.Where(p => p.Status == status);
        }
        else
        {
            query = query.Where(p => p.Status != PersonaStatus.Archived);
        }

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var personas = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get liked persona IDs
        var personaIds = personas.Select(p => p.Id).ToList();
        var likedIds = await _context.PersonaLikes
            .Where(l => l.UserId == _currentUser.UserId && personaIds.Contains(l.PersonaId))
            .Select(l => l.PersonaId)
            .ToListAsync(cancellationToken);

        var dtos = personas.Select(p => new PersonaDto(
            p.Id, p.CreatorId, p.Name, p.Description, p.Bio, p.ImagePath,
            p.PersonalityTraits, p.LanguageStyle, p.Expertise, p.Tags,
            p.VoiceId, p.VoiceSettings, p.IsPublic, p.IsMarketplace,
            p.Status.ToString(), p.LikeCount, p.ViewCount, p.CloneCount,
            p.CreatedAt, p.UpdatedAt, true, likedIds.Contains(p.Id))).ToList();

        return Result<PersonaListDto>.Success(new PersonaListDto(dtos, total, request.Page, request.PageSize));
    }
}
