using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Email, string Password, string? DisplayName = null)
    : IRequest<Result<TokenDto>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<TokenDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTime;

    public RegisterCommandHandler(
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

    public async Task<Result<TokenDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower(), cancellationToken);

        if (existingUser != null)
            return Result<TokenDto>.Failure("Email already registered", 400);

        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            AuthProvider = AuthProvider.Email,
            SubscriptionTier = SubscriptionTier.Free,
            IsActive = true,
            EmailVerified = false,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
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

        var token = _jwtService.GenerateAccessToken(user);

        return Result<TokenDto>.Created(new TokenDto(token, "bearer", user.Id));
    }
}
