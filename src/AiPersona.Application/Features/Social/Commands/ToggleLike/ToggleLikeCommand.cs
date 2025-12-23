using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;
using AiPersona.Domain.Entities;

namespace AiPersona.Application.Features.Social.Commands.ToggleLike;

public record ToggleLikeCommand(Guid PersonaId) : IRequest<Result<LikeResultDto>>;

public class ToggleLikeCommandHandler : IRequestHandler<ToggleLikeCommand, Result<LikeResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ToggleLikeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<LikeResultDto>> Handle(ToggleLikeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<LikeResultDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<LikeResultDto>.Failure("Persona not found", 404);

        var existingLike = await _context.PersonaLikes
            .FirstOrDefaultAsync(l => l.PersonaId == request.PersonaId && l.UserId == _currentUser.UserId, cancellationToken);

        bool isLiked;
        if (existingLike != null)
        {
            _context.PersonaLikes.Remove(existingLike);
            persona.LikeCount = Math.Max(0, persona.LikeCount - 1);
            isLiked = false;
        }
        else
        {
            var like = new PersonaLike
            {
                Id = Guid.NewGuid(),
                PersonaId = request.PersonaId,
                UserId = _currentUser.UserId.Value,
                CreatedAt = _dateTime.UtcNow
            };
            _context.PersonaLikes.Add(like);
            persona.LikeCount++;
            isLiked = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<LikeResultDto>.Success(new LikeResultDto(isLiked, persona.LikeCount));
    }
}
