using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;

namespace AiPersona.Application.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(string? DisplayName = null, string? PhotoUrl = null)
    : IRequest<Result<UserDto>>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public UpdateProfileCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UserDto>.Failure("Unauthorized", 401);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
            return Result<UserDto>.Failure("User not found", 404);

        if (request.DisplayName != null)
            user.DisplayName = request.DisplayName;

        if (request.PhotoUrl != null)
            user.PhotoUrl = request.PhotoUrl;

        user.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.PhotoUrl,
            user.SubscriptionTier.ToString(),
            user.IsActive,
            user.EmailVerified,
            user.AuthProvider.ToString(),
            user.CreatedAt,
            user.LastLogin));
    }
}
