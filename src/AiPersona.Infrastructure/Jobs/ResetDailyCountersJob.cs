using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AiPersona.Infrastructure.Persistence;

namespace AiPersona.Infrastructure.Jobs;

public class ResetDailyCountersJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResetDailyCountersJob> _logger;

    public ResetDailyCountersJob(
        ApplicationDbContext context,
        ILogger<ResetDailyCountersJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("Starting daily counter reset");

        var today = DateTime.UtcNow;

        // Reset message counters for all users
        var updatedCount = await _context.UsageTrackings
            .Where(u => u.MessagesCountResetAt.Date < today.Date)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.MessagesToday, 0)
                .SetProperty(u => u.MessagesCountResetAt, today));

        _logger.LogInformation("Reset daily counters for {Count} users", updatedCount);
    }
}
