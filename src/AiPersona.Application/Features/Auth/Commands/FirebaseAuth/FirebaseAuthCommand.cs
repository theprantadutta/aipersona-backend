using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Auth.Commands.FirebaseAuth;

public record FirebaseAuthCommand(string FirebaseToken) : IRequest<Result<TokenDto>>;

public class FirebaseAuthCommandValidator : AbstractValidator<FirebaseAuthCommand>
{
    public FirebaseAuthCommandValidator()
    {
        RuleFor(x => x.FirebaseToken).NotEmpty().WithMessage("Firebase token is required");
    }
}

public class FirebaseAuthCommandHandler : IRequestHandler<FirebaseAuthCommand, Result<TokenDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFirebaseAuthService _firebaseAuth;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTime;

    public FirebaseAuthCommandHandler(
        IApplicationDbContext context,
        IFirebaseAuthService firebaseAuth,
        IJwtService jwtService,
        IDateTimeService dateTime)
    {
        _context = context;
        _firebaseAuth = firebaseAuth;
        _jwtService = jwtService;
        _dateTime = dateTime;
    }

    public async Task<Result<TokenDto>> Handle(FirebaseAuthCommand request, CancellationToken cancellationToken)
    {
        var firebaseUser = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseToken);
        if (firebaseUser == null)
            return Result<TokenDto>.Failure("Invalid Firebase token", 401);

        if (string.IsNullOrEmpty(firebaseUser.Email))
            return Result<TokenDto>.Failure("Firebase account has no email", 400);

        // Check if user exists by Firebase UID
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUser.Uid, cancellationToken);

        if (user != null)
        {
            // Generate tokens for existing user
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshExpireDays = _jwtService.GetRefreshTokenExpireDays();
            var accessExpireMinutes = _jwtService.GetAccessTokenExpireMinutes();

            user.RefreshTokenHash = _jwtService.HashRefreshToken(refreshToken);
            user.RefreshTokenExpiresAt = _dateTime.UtcNow.AddDays(refreshExpireDays);
            user.LastLogin = _dateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result<TokenDto>.Success(new TokenDto(
                accessToken,
                refreshToken,
                "bearer",
                user.Id,
                _dateTime.UtcNow.AddMinutes(accessExpireMinutes),
                user.RefreshTokenExpiresAt.Value));
        }

        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == firebaseUser.Email.ToLower(), cancellationToken);

        if (existingUser != null)
        {
            return Result<TokenDto>.Failure("Account with this email already exists. Please link your account.", 409);
        }

        // Create new user
        var isGoogle = firebaseUser.Provider == "google.com";
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshExpireDays = _jwtService.GetRefreshTokenExpireDays();
        var newAccessExpireMinutes = _jwtService.GetAccessTokenExpireMinutes();

        user = new User
        {
            Email = firebaseUser.Email.ToLower(),
            FirebaseUid = firebaseUser.Uid,
            GoogleId = isGoogle ? firebaseUser.Uid : null,
            DisplayName = firebaseUser.DisplayName,
            PhotoUrl = firebaseUser.PhotoUrl,
            AuthProvider = isGoogle ? AuthProvider.Google : AuthProvider.Firebase,
            SubscriptionTier = SubscriptionTier.Free,
            IsActive = true,
            EmailVerified = firebaseUser.EmailVerified,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow,
            LastLogin = _dateTime.UtcNow,
            RefreshTokenHash = _jwtService.HashRefreshToken(newRefreshToken),
            RefreshTokenExpiresAt = _dateTime.UtcNow.AddDays(newRefreshExpireDays)
        };

        _context.Users.Add(user);

        var usageTracking = new UsageTracking
        {
            UserId = user.Id,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };
        _context.UsageTrackings.Add(usageTracking);

        await _context.SaveChangesAsync(cancellationToken);

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        return Result<TokenDto>.Success(new TokenDto(
            newAccessToken,
            newRefreshToken,
            "bearer",
            user.Id,
            _dateTime.UtcNow.AddMinutes(newAccessExpireMinutes),
            user.RefreshTokenExpiresAt!.Value));
    }
}
