using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;
using AiPersona.Domain.Entities;

namespace AiPersona.Application.Features.Social.Commands.ToggleFollow;

public record ToggleFollowCommand(Guid UserId) : IRequest<Result<FollowResultDto>>;

public class ToggleFollowCommandHandler : IRequestHandler<ToggleFollowCommand, Result<FollowResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ToggleFollowCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<FollowResultDto>> Handle(ToggleFollowCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<FollowResultDto>.Failure("Unauthorized", 401);

        if (request.UserId == _currentUser.UserId)
            return Result<FollowResultDto>.Failure("Cannot follow yourself", 400);

        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (targetUser == null)
            return Result<FollowResultDto>.Failure("User not found", 404);

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        var existingFollow = await _context.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == _currentUser.UserId && f.FollowingId == request.UserId, cancellationToken);

        bool isFollowing;
        if (existingFollow != null)
        {
            _context.UserFollows.Remove(existingFollow);
            targetUser.FollowerCount = Math.Max(0, targetUser.FollowerCount - 1);
            if (currentUser != null)
                currentUser.FollowingCount = Math.Max(0, currentUser.FollowingCount - 1);
            isFollowing = false;
        }
        else
        {
            var follow = new UserFollow
            {
                Id = Guid.NewGuid(),
                FollowerId = _currentUser.UserId.Value,
                FollowingId = request.UserId,
                CreatedAt = _dateTime.UtcNow
            };
            _context.UserFollows.Add(follow);
            targetUser.FollowerCount++;
            if (currentUser != null)
                currentUser.FollowingCount++;
            isFollowing = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<FollowResultDto>.Success(new FollowResultDto(
            isFollowing,
            targetUser.FollowerCount,
            targetUser.FollowingCount));
    }
}
