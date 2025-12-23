using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;
using AiPersona.Domain.Entities;

namespace AiPersona.Application.Features.Social.Commands.ToggleFavorite;

public record ToggleFavoriteCommand(Guid PersonaId) : IRequest<Result<FavoriteResultDto>>;

public class ToggleFavoriteCommandHandler : IRequestHandler<ToggleFavoriteCommand, Result<FavoriteResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ToggleFavoriteCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<FavoriteResultDto>> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<FavoriteResultDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<FavoriteResultDto>.Failure("Persona not found", 404);

        var existingFavorite = await _context.PersonaFavorites
            .FirstOrDefaultAsync(f => f.PersonaId == request.PersonaId && f.UserId == _currentUser.UserId, cancellationToken);

        bool isFavorited;
        if (existingFavorite != null)
        {
            _context.PersonaFavorites.Remove(existingFavorite);
            persona.FavoriteCount = Math.Max(0, persona.FavoriteCount - 1);
            isFavorited = false;
        }
        else
        {
            var favorite = new PersonaFavorite
            {
                Id = Guid.NewGuid(),
                PersonaId = request.PersonaId,
                UserId = _currentUser.UserId.Value,
                CreatedAt = _dateTime.UtcNow
            };
            _context.PersonaFavorites.Add(favorite);
            persona.FavoriteCount++;
            isFavorited = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<FavoriteResultDto>.Success(new FavoriteResultDto(isFavorited, persona.FavoriteCount));
    }
}
