using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;

namespace AiPersona.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<TokenDto>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTime;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeService dateTime)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTime = dateTime;
    }

    public async Task<Result<TokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Hash the incoming refresh token to compare with stored hash
        var incomingHash = _jwtService.HashRefreshToken(request.RefreshToken);

        // Find user with matching refresh token hash
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == incomingHash, cancellationToken);

        if (user == null)
            return Result<TokenDto>.Failure("Invalid refresh token", 401);

        // Check if refresh token is expired
        if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < _dateTime.UtcNow)
            return Result<TokenDto>.Failure("Refresh token expired", 401);

        // Check if user is active
        if (!user.IsActive)
            return Result<TokenDto>.Failure("Account is suspended", 403);

        // Generate new tokens (token rotation for security)
        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpireDays = _jwtService.GetRefreshTokenExpireDays();
        var accessExpireMinutes = _jwtService.GetAccessTokenExpireMinutes();

        // Update user with new refresh token (rotation)
        user.RefreshTokenHash = _jwtService.HashRefreshToken(newRefreshToken);
        user.RefreshTokenExpiresAt = _dateTime.UtcNow.AddDays(refreshExpireDays);
        user.LastLogin = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TokenDto>.Success(new TokenDto(
            accessToken,
            newRefreshToken,
            "bearer",
            user.Id,
            _dateTime.UtcNow.AddMinutes(accessExpireMinutes),
            user.RefreshTokenExpiresAt.Value));
    }
}
