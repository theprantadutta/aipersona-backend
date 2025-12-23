using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.CheckLiked;

public record CheckLikedQuery(Guid PersonaId) : IRequest<Result<CheckLikedDto>>;

public class CheckLikedQueryHandler : IRequestHandler<CheckLikedQuery, Result<CheckLikedDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CheckLikedQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CheckLikedDto>> Handle(CheckLikedQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<CheckLikedDto>.Failure("Unauthorized", 401);

        var isLiked = await _context.PersonaLikes
            .AnyAsync(l => l.PersonaId == request.PersonaId && l.UserId == _currentUser.UserId, cancellationToken);

        return Result<CheckLikedDto>.Success(new CheckLikedDto(isLiked));
    }
}
