using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Infrastructure.Services;

public interface ITestUserSeederService
{
    Task<TestUserSeederResult> SeedTestUsersAsync(CancellationToken cancellationToken = default);
}

public record TestUserSeederResult(
    bool FreeUserCreated,
    bool PremiumUserCreated,
    string FreeUserEmail,
    string PremiumUserEmail,
    string Password,
    List<string> Messages);

public class TestUserSeederService : ITestUserSeederService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<TestUserSeederService> _logger;

    // Test user credentials
    private const string FreeUserEmail = "testfree@aipersona.app";
    private const string PremiumUserEmail = "testpremium@aipersona.app";
    private const string TestPassword = "Test@123";

    public TestUserSeederService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<TestUserSeederService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<TestUserSeederResult> SeedTestUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("======================================================================");
        _logger.LogInformation("Test User Seeding - Creating Free and Premium test accounts");
        _logger.LogInformation("======================================================================");

        var messages = new List<string>();
        var freeUserCreated = false;
        var premiumUserCreated = false;

        // Create Free User
        var existingFreeUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == FreeUserEmail, cancellationToken);

        if (existingFreeUser != null)
        {
            messages.Add($"Free user already exists: {FreeUserEmail}");
            _logger.LogInformation("Free user already exists: {Email}", FreeUserEmail);
        }
        else
        {
            var freeUser = new User
            {
                Email = FreeUserEmail,
                PasswordHash = _passwordHasher.HashPassword(TestPassword),
                DisplayName = "Test Free User",
                IsActive = true,
                IsAdmin = false,
                SubscriptionTier = SubscriptionTier.Free,
                AuthProvider = AuthProvider.Email,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(freeUser);
            await _context.SaveChangesAsync(cancellationToken);

            // Create usage tracking
            var freeUsage = new UsageTracking { UserId = freeUser.Id };
            _context.UsageTrackings.Add(freeUsage);
            await _context.SaveChangesAsync(cancellationToken);

            freeUserCreated = true;
            messages.Add($"Created free user: {FreeUserEmail}");
            _logger.LogInformation("Created free user: {Email}", FreeUserEmail);
        }

        // Create Premium User
        var existingPremiumUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == PremiumUserEmail, cancellationToken);

        if (existingPremiumUser != null)
        {
            // Update to ensure premium status is active
            existingPremiumUser.SubscriptionTier = SubscriptionTier.Premium;
            existingPremiumUser.SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1);
            existingPremiumUser.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            messages.Add($"Premium user already exists, updated subscription: {PremiumUserEmail}");
            _logger.LogInformation("Premium user already exists, updated subscription: {Email}", PremiumUserEmail);
        }
        else
        {
            var premiumUser = new User
            {
                Email = PremiumUserEmail,
                PasswordHash = _passwordHasher.HashPassword(TestPassword),
                DisplayName = "Test Premium User",
                IsActive = true,
                IsAdmin = false,
                SubscriptionTier = SubscriptionTier.Premium,
                SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1), // 1 year subscription
                AuthProvider = AuthProvider.Email,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(premiumUser);
            await _context.SaveChangesAsync(cancellationToken);

            // Create usage tracking
            var premiumUsage = new UsageTracking { UserId = premiumUser.Id };
            _context.UsageTrackings.Add(premiumUsage);
            await _context.SaveChangesAsync(cancellationToken);

            premiumUserCreated = true;
            messages.Add($"Created premium user: {PremiumUserEmail}");
            _logger.LogInformation("Created premium user: {Email}", PremiumUserEmail);
        }

        _logger.LogInformation("======================================================================");
        _logger.LogInformation("Test User Seeding Complete!");
        _logger.LogInformation("   Free User: {Email}", FreeUserEmail);
        _logger.LogInformation("   Premium User: {Email}", PremiumUserEmail);
        _logger.LogInformation("   Password (both): {Password}", TestPassword);
        _logger.LogInformation("======================================================================");

        return new TestUserSeederResult(
            freeUserCreated,
            premiumUserCreated,
            FreeUserEmail,
            PremiumUserEmail,
            TestPassword,
            messages);
    }
}
