using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.GetPersonaStats;

public record GetPersonaStatsQuery(Guid PersonaId) : IRequest<Result<PersonaStatsDto>>;

public class GetPersonaStatsQueryHandler : IRequestHandler<GetPersonaStatsQuery, Result<PersonaStatsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetPersonaStatsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PersonaStatsDto>> Handle(GetPersonaStatsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<PersonaStatsDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<PersonaStatsDto>.Failure("Persona not found", 404);

        var viewCount = await _context.PersonaViews
            .Where(v => v.PersonaId == request.PersonaId)
            .CountAsync(cancellationToken);

        var chatCount = await _context.ChatSessions
            .Where(s => s.PersonaId == request.PersonaId)
            .CountAsync(cancellationToken);

        var messageCount = await _context.ChatMessages
            .Where(m => m.Session.PersonaId == request.PersonaId)
            .CountAsync(cancellationToken);

        return Result<PersonaStatsDto>.Success(new PersonaStatsDto(
            persona.Id,
            persona.LikeCount,
            persona.FavoriteCount,
            viewCount,
            chatCount,
            messageCount));
    }
}
