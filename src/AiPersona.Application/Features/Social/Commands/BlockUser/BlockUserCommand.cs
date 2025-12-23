using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;
using AiPersona.Domain.Entities;

namespace AiPersona.Application.Features.Social.Commands.BlockUser;

public record BlockUserCommand(Guid UserId, string? Reason = null) : IRequest<Result<BlockResultDto>>;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, Result<BlockResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public BlockUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<BlockResultDto>> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<BlockResultDto>.Failure("Unauthorized", 401);

        if (request.UserId == _currentUser.UserId)
            return Result<BlockResultDto>.Failure("Cannot block yourself", 400);

        var targetUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (targetUser == null)
            return Result<BlockResultDto>.Failure("User not found", 404);

        var existingBlock = await _context.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == _currentUser.UserId && b.BlockedId == request.UserId, cancellationToken);

        if (existingBlock != null)
            return Result<BlockResultDto>.Success(new BlockResultDto(true, "User already blocked"));

        var block = new UserBlock
        {
            Id = Guid.NewGuid(),
            BlockerId = _currentUser.UserId.Value,
            BlockedId = request.UserId,
            Reason = request.Reason,
            CreatedAt = _dateTime.UtcNow
        };
        _context.UserBlocks.Add(block);

        // Remove any follow relationships
        var followToRemove = await _context.UserFollows
            .Where(f => (f.FollowerId == _currentUser.UserId && f.FollowingId == request.UserId) ||
                        (f.FollowerId == request.UserId && f.FollowingId == _currentUser.UserId))
            .ToListAsync(cancellationToken);

        _context.UserFollows.RemoveRange(followToRemove);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<BlockResultDto>.Success(new BlockResultDto(true, "User blocked successfully"));
    }
}
