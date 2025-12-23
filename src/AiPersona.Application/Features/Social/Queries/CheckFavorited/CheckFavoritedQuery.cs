using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.CheckFavorited;

public record CheckFavoritedQuery(Guid PersonaId) : IRequest<Result<CheckFavoritedDto>>;

public class CheckFavoritedQueryHandler : IRequestHandler<CheckFavoritedQuery, Result<CheckFavoritedDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CheckFavoritedQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CheckFavoritedDto>> Handle(CheckFavoritedQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<CheckFavoritedDto>.Failure("Unauthorized", 401);

        var isFavorited = await _context.PersonaFavorites
            .AnyAsync(f => f.PersonaId == request.PersonaId && f.UserId == _currentUser.UserId, cancellationToken);

        return Result<CheckFavoritedDto>.Success(new CheckFavoritedDto(isFavorited));
    }
}
