using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.GetFavorites;

public record GetFavoritesQuery(int Limit = 50, int Offset = 0) : IRequest<Result<FavoritesListDto>>;

public class GetFavoritesQueryHandler : IRequestHandler<GetFavoritesQuery, Result<FavoritesListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetFavoritesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<FavoritesListDto>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<FavoritesListDto>.Failure("Unauthorized", 401);

        var query = _context.PersonaFavorites
            .Include(f => f.Persona)
            .Where(f => f.UserId == _currentUser.UserId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var favorites = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var dtos = favorites.Select(f => new FavoritePersonaDto(
            f.PersonaId,
            f.Persona?.Name ?? "Unknown",
            f.Persona?.ImagePath,
            f.Persona?.Description,
            f.Persona?.LikeCount ?? 0,
            f.CreatedAt)).ToList();

        return Result<FavoritesListDto>.Success(new FavoritesListDto(dtos, total, request.Limit, request.Offset));
    }
}
