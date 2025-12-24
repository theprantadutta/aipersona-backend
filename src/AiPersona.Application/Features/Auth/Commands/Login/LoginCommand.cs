using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;

namespace AiPersona.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<TokenDto>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<TokenDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTime;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IDateTimeService dateTime)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _dateTime = dateTime;
    }

    public async Task<Result<TokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower(), cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return Result<TokenDto>.Failure("Invalid email or password", 401);

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Result<TokenDto>.Failure("Invalid email or password", 401);

        if (!user.IsActive)
            return Result<TokenDto>.Failure("Account is suspended", 403);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpireDays = _jwtService.GetRefreshTokenExpireDays();
        var accessExpireMinutes = _jwtService.GetAccessTokenExpireMinutes();

        // Store refresh token hash
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
}
