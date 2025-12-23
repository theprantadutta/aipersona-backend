using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Social.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUserProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UserProfileDto>.Failure("Unauthorized", 401);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return Result<UserProfileDto>.Failure("User not found", 404);

        var personaCount = await _context.Personas
            .Where(p => p.CreatorId == request.UserId && p.IsPublic && p.Status == PersonaStatus.Active)
            .CountAsync(cancellationToken);

        var isFollowing = await _context.UserFollows
            .AnyAsync(f => f.FollowerId == _currentUser.UserId && f.FollowingId == request.UserId, cancellationToken);

        var isBlocked = await _context.UserBlocks
            .AnyAsync(b => b.BlockerId == _currentUser.UserId && b.BlockedId == request.UserId, cancellationToken);

        return Result<UserProfileDto>.Success(new UserProfileDto(
            user.Id,
            user.DisplayName ?? user.Email,
            user.ProfileImage,
            user.Bio,
            user.FollowerCount,
            user.FollowingCount,
            personaCount,
            isFollowing,
            isBlocked,
            user.CreatedAt));
    }
}
