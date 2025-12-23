using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Auth.Commands.LinkGoogle;

public record LinkGoogleCommand(string FirebaseToken, string Password) : IRequest<Result<TokenDto>>;

public class LinkGoogleCommandValidator : AbstractValidator<LinkGoogleCommand>
{
    public LinkGoogleCommandValidator()
    {
        RuleFor(x => x.FirebaseToken).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LinkGoogleCommandHandler : IRequestHandler<LinkGoogleCommand, Result<TokenDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFirebaseAuthService _firebaseAuth;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTime;

    public LinkGoogleCommandHandler(
        IApplicationDbContext context,
        IFirebaseAuthService firebaseAuth,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IDateTimeService dateTime)
    {
        _context = context;
        _firebaseAuth = firebaseAuth;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _dateTime = dateTime;
    }

    public async Task<Result<TokenDto>> Handle(LinkGoogleCommand request, CancellationToken cancellationToken)
    {
        var firebaseUser = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseToken);
        if (firebaseUser == null)
            return Result<TokenDto>.Failure("Invalid Firebase token", 401);

        if (string.IsNullOrEmpty(firebaseUser.Email))
            return Result<TokenDto>.Failure("Firebase account has no email", 400);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == firebaseUser.Email.ToLower(), cancellationToken);

        if (user == null)
            return Result<TokenDto>.Failure("No account found with this email", 404);

        if (string.IsNullOrEmpty(user.PasswordHash))
            return Result<TokenDto>.Failure("Account has no password set", 400);

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Result<TokenDto>.Failure("Incorrect password", 401);

        // Check if Firebase UID is already linked to another account
        var existingFirebaseUser = await _context.Users
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUser.Uid && u.Id != user.Id, cancellationToken);

        if (existingFirebaseUser != null)
            return Result<TokenDto>.Failure("This Google account is already linked to another user", 409);

        // Link the accounts
        user.FirebaseUid = firebaseUser.Uid;
        user.GoogleId = firebaseUser.Uid;
        user.DisplayName = firebaseUser.DisplayName ?? user.DisplayName;
        user.PhotoUrl = firebaseUser.PhotoUrl ?? user.PhotoUrl;
        user.EmailVerified = firebaseUser.EmailVerified;
        user.AuthProvider = firebaseUser.Provider == "google.com" ? AuthProvider.Google : AuthProvider.Firebase;
        user.LastLogin = _dateTime.UtcNow;
        user.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var token = _jwtService.GenerateAccessToken(user);
        return Result<TokenDto>.Success(new TokenDto(token, "bearer", user.Id));
    }
}
