using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;
using AiPersona.Infrastructure.Persistence;

namespace AiPersona.Infrastructure.Jobs;

public class CheckSubscriptionExpirationsJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckSubscriptionExpirationsJob> _logger;

    public CheckSubscriptionExpirationsJob(
        ApplicationDbContext context,
        ILogger<CheckSubscriptionExpirationsJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("Starting subscription expiration check");

        var now = DateTime.UtcNow;

        // Find users with expired subscriptions
        var expiredUsers = await _context.Users
            .Where(u => u.SubscriptionTier != SubscriptionTier.Free &&
                       u.SubscriptionExpiresAt.HasValue &&
                       u.SubscriptionExpiresAt < now)
            .ToListAsync();

        if (!expiredUsers.Any())
        {
            _logger.LogInformation("No expired subscriptions found");
            return;
        }

        foreach (var user in expiredUsers)
        {
            var previousTier = user.SubscriptionTier;
            user.SubscriptionTier = SubscriptionTier.Free;
            user.SubscriptionExpiresAt = null;
            user.UpdatedAt = now;

            // Log the expiration event
            var expirationEvent = new SubscriptionEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EventType = SubscriptionEventType.Expired,
                SubscriptionTier = previousTier,
                ExpiresAt = now,
                CreatedAt = now
            };
            _context.SubscriptionEvents.Add(expirationEvent);

            _logger.LogInformation(
                "Subscription expired for user {UserId}, downgraded from {OldTier} to Free",
                user.Id, previousTier);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed {Count} expired subscriptions", expiredUsers.Count);
    }
}
