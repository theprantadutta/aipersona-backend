using AiPersona.Domain.Entities;

namespace AiPersona.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    (Guid userId, string email)? ValidateToken(string token);
}
