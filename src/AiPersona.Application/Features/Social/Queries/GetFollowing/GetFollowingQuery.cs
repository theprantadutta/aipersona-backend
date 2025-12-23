using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.GetFollowing;

public record GetFollowingQuery(Guid UserId, int Limit = 50, int Offset = 0) : IRequest<Result<UserListDto>>;

public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, Result<UserListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetFollowingQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UserListDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UserListDto>.Failure("Unauthorized", 401);

        var query = _context.UserFollows
            .Include(f => f.Following)
            .Where(f => f.FollowerId == request.UserId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var following = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var followingIds = following.Select(f => f.FollowingId).ToList();
        var currentUserFollowing = await _context.UserFollows
            .Where(f => f.FollowerId == _currentUser.UserId && followingIds.Contains(f.FollowingId))
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        var dtos = following.Select(f => new UserListItemDto(
            f.FollowingId,
            f.Following?.DisplayName ?? f.Following?.Email ?? "Unknown",
            f.Following?.ProfileImage,
            currentUserFollowing.Contains(f.FollowingId),
            f.CreatedAt)).ToList();

        return Result<UserListDto>.Success(new UserListDto(dtos, total, request.Limit, request.Offset));
    }
}
