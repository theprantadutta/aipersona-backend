using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Auth.Commands.UnlinkGoogle;

public record UnlinkGoogleCommand : IRequest<Result>;

public class UnlinkGoogleCommandHandler : IRequestHandler<UnlinkGoogleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public UnlinkGoogleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(UnlinkGoogleCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result.Failure("Unauthorized", 401);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
            return Result.Failure("User not found", 404);

        if (string.IsNullOrEmpty(user.PasswordHash))
            return Result.Failure("Cannot unlink Google account. Please set a password first.", 400);

        if (string.IsNullOrEmpty(user.FirebaseUid) && string.IsNullOrEmpty(user.GoogleId))
            return Result.Failure("No Google account is linked", 400);

        user.FirebaseUid = null;
        user.GoogleId = null;
        user.AuthProvider = AuthProvider.Email;
        user.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
