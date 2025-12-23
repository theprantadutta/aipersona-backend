namespace AiPersona.Application.Common.Interfaces;

public record FirebaseUserInfo(
    string Uid,
    string? Email,
    string? DisplayName,
    string? PhotoUrl,
    bool EmailVerified,
    string Provider);

public interface IFirebaseAuthService
{
    Task<FirebaseUserInfo?> VerifyIdTokenAsync(string idToken);
}
