using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Personas.Queries.GetPersona;

public record GetPersonaQuery(Guid PersonaId) : IRequest<Result<PersonaDto>>;

public class GetPersonaQueryHandler : IRequestHandler<GetPersonaQuery, Result<PersonaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetPersonaQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<PersonaDto>> Handle(GetPersonaQuery request, CancellationToken cancellationToken)
    {
        var persona = await _context.Personas
            .Include(p => p.Creator)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<PersonaDto>.Failure("Persona not found", 404);

        var isOwner = _currentUser.UserId.HasValue && persona.CreatorId == _currentUser.UserId;

        // Check access
        if (!isOwner && !persona.IsPublic && persona.Status != PersonaStatus.Active)
            return Result<PersonaDto>.Failure("Access denied", 403);

        // Increment view count
        if (!isOwner)
        {
            var trackedPersona = await _context.Personas.FindAsync([request.PersonaId], cancellationToken);
            if (trackedPersona != null)
            {
                trackedPersona.ViewCount++;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Check if liked
        var isLiked = false;
        if (_currentUser.UserId.HasValue)
        {
            isLiked = await _context.PersonaLikes
                .AnyAsync(l => l.UserId == _currentUser.UserId && l.PersonaId == request.PersonaId, cancellationToken);
        }

        return Result<PersonaDto>.Success(new PersonaDto(
            persona.Id, persona.CreatorId, persona.Name, persona.Description, persona.Bio, persona.ImagePath,
            persona.PersonalityTraits, persona.LanguageStyle, persona.Expertise, persona.Tags,
            persona.VoiceId, persona.VoiceSettings, persona.IsPublic, persona.IsMarketplace,
            persona.Status.ToString(), persona.LikeCount, persona.ViewCount, persona.CloneCount,
            persona.CreatedAt, persona.UpdatedAt, isOwner, isLiked, persona.Creator?.DisplayName));
    }
}
