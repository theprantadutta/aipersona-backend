using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Personas.Queries.GetTrendingPersonas;

public record GetTrendingPersonasQuery(string Timeframe = "week", int Limit = 20)
    : IRequest<Result<TrendingPersonasDto>>;

public class GetTrendingPersonasQueryHandler : IRequestHandler<GetTrendingPersonasQuery, Result<TrendingPersonasDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetTrendingPersonasQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<TrendingPersonasDto>> Handle(GetTrendingPersonasQuery request, CancellationToken cancellationToken)
    {
        var since = request.Timeframe.ToLower() switch
        {
            "day" => _dateTime.UtcNow.AddDays(-1),
            "week" => _dateTime.UtcNow.AddDays(-7),
            "month" => _dateTime.UtcNow.AddMonths(-1),
            _ => _dateTime.UtcNow.AddDays(-7)
        };

        // Get personas with recent activity (likes, views)
        var personas = await _context.Personas
            .Include(p => p.Creator)
            .Where(p => p.IsPublic && p.Status == PersonaStatus.Active)
            .OrderByDescending(p => p.LikeCount)
            .ThenByDescending(p => p.ViewCount)
            .Take(request.Limit)
            .AsNoTracking()
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

        return Result<TrendingPersonasDto>.Success(new TrendingPersonasDto(dtos, request.Timeframe));
    }
}
