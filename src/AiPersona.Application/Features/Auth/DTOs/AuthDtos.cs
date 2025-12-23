namespace AiPersona.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string TokenType,
    Guid UserId,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? PhotoUrl,
    string SubscriptionTier,
    bool IsActive,
    bool EmailVerified,
    string? AuthProvider,
    DateTime CreatedAt,
    DateTime? LastLogin,
    bool IsPremium,
    DateTime? SubscriptionExpiresAt);

public record AuthProvidersDto(
    List<string> Providers,
    bool HasPassword,
    bool HasGoogle);

public record TokenDto(
    string AccessToken,
    string TokenType,
    Guid UserId);
