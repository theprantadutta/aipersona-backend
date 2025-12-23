using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiPersona.Domain.Enums;
using AiPersona.Infrastructure.Persistence;

namespace AiPersona.Infrastructure.Jobs;

public class CleanupFreeTierHistoryJob
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CleanupFreeTierHistoryJob> _logger;

    public CleanupFreeTierHistoryJob(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<CleanupFreeTierHistoryJob> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("Starting cleanup of free tier chat history");

        var retentionDays = _configuration.GetValue<int>("FreeTier:HistoryRetentionDays", 7);
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        // Get free tier users
        var freeUserIds = await _context.Users
            .Where(u => u.SubscriptionTier == SubscriptionTier.Free)
            .Select(u => u.Id)
            .ToListAsync();

        if (!freeUserIds.Any())
        {
            _logger.LogInformation("No free tier users found");
            return;
        }

        // Get sessions older than retention period for free users
        var sessionsToClean = await _context.ChatSessions
            .Where(s => freeUserIds.Contains(s.UserId) &&
                       s.CreatedAt < cutoffDate &&
                       s.Status != ChatSessionStatus.Deleted)
            .ToListAsync();

        var sessionIds = sessionsToClean.Select(s => s.Id).ToList();

        if (!sessionIds.Any())
        {
            _logger.LogInformation("No old sessions found to clean");
            return;
        }

        // Delete old messages
        var deletedMessages = await _context.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId))
            .ExecuteDeleteAsync();

        // Delete old attachments
        var deletedAttachments = await _context.MessageAttachments
            .Where(a => sessionIds.Contains(a.Message.SessionId))
            .ExecuteDeleteAsync();

        // Mark sessions as deleted
        foreach (var session in sessionsToClean)
        {
            session.Status = ChatSessionStatus.Deleted;
            session.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Cleanup complete: {SessionCount} sessions, {MessageCount} messages, {AttachmentCount} attachments",
            sessionsToClean.Count, deletedMessages, deletedAttachments);
    }
}
