using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.GetFollowers;

public record GetFollowersQuery(Guid UserId, int Limit = 50, int Offset = 0) : IRequest<Result<UserListDto>>;

public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, Result<UserListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetFollowersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UserListDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UserListDto>.Failure("Unauthorized", 401);

        var query = _context.UserFollows
            .Include(f => f.Follower)
            .Where(f => f.FollowingId == request.UserId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var followers = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var followerIds = followers.Select(f => f.FollowerId).ToList();
        var currentUserFollowing = await _context.UserFollows
            .Where(f => f.FollowerId == _currentUser.UserId && followerIds.Contains(f.FollowingId))
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        var dtos = followers.Select(f => new UserListItemDto(
            f.FollowerId,
            f.Follower?.DisplayName ?? f.Follower?.Email ?? "Unknown",
            f.Follower?.ProfileImage,
            currentUserFollowing.Contains(f.FollowerId),
            f.CreatedAt)).ToList();

        return Result<UserListDto>.Success(new UserListDto(dtos, total, request.Limit, request.Offset));
    }
}
