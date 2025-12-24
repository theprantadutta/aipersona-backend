using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;

namespace AiPersona.Application.Features.Auth.Commands.RevokeRefreshToken;

public record RevokeRefreshTokenCommand : IRequest<Result>;

public class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public RevokeRefreshTokenCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null)
            return Result.Failure("Not authenticated", 401);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result.Failure("User not found", 404);

        // Invalidate the refresh token
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
