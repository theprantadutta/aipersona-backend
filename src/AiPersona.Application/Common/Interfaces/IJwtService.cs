using AiPersona.Domain.Entities;

namespace AiPersona.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    bool VerifyRefreshToken(string refreshToken, string hash);
    (Guid userId, string email)? ValidateToken(string token);
    int GetRefreshTokenExpireDays();
    int GetAccessTokenExpireMinutes();
}
