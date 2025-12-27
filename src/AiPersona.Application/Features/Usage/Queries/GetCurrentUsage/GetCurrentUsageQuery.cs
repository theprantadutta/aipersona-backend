using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Usage.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Usage.Queries.GetCurrentUsage;

public record GetCurrentUsageQuery : IRequest<Result<CurrentUsageDto>>;

public class GetCurrentUsageQueryHandler : IRequestHandler<GetCurrentUsageQuery, Result<CurrentUsageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetCurrentUsageQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<CurrentUsageDto>> Handle(GetCurrentUsageQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<CurrentUsageDto>.Failure("Unauthorized", 401);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
            return Result<CurrentUsageDto>.Failure("User not found", 404);

        var today = _dateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Count ALL messages (user + AI) sent today, excluding system messages
        var userSessionIds = await _context.ChatSessions
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var messagesToday = await _context.ChatMessages
            .Where(m => userSessionIds.Contains(m.SessionId)
                && m.MessageType != MessageType.System  // Exclude system/greeting messages
                && m.CreatedAt >= today
                && m.CreatedAt < tomorrow)
            .CountAsync(cancellationToken);

        var personaCount = await _context.Personas
            .Where(p => p.CreatorId == _currentUser.UserId && p.Status != PersonaStatus.Archived)
            .CountAsync(cancellationToken);

        var storageUsed = await _context.UploadedFiles
            .Where(f => f.UserId == _currentUser.UserId)
            .SumAsync(f => f.FileSize, cancellationToken);

        var (messageLimit, personaLimit, storageLimitMb, historyDays) = GetLimitsForTier(user.SubscriptionTier);

        var nextReset = today.AddDays(1);
        var isPremium = user.SubscriptionTier != SubscriptionTier.Free;
        var storageUsedMb = storageUsed / (1024.0 * 1024.0);

        return Result<CurrentUsageDto>.Success(new CurrentUsageDto(
            messagesToday,
            messageLimit,
            personaCount,           // PersonasCount in DTO
            personaLimit,
            storageUsed,
            storageUsedMb,          // NEW: StorageUsedMb
            storageLimitMb * 1024 * 1024,
            historyDays,
            historyDays,
            user.SubscriptionTier.ToString(),
            isPremium,              // NEW: IsPremium
            nextReset));
    }

    /// <summary>
    /// Get subscription tier limits. Returns (messageLimit, personaLimit, storageMb, historyDays).
    /// -1 means unlimited. MUST match SendMessageCommand.GetLimitsForTier() and GetPlansQuery.
    /// </summary>
    private static (int messageLimit, int personaLimit, int storageMb, int historyDays) GetLimitsForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Pro => (-1, -1, 102400, -1),      // Unlimited messages/personas, 100GB storage
            SubscriptionTier.Premium => (500, -1, 10240, -1),  // 500 messages/day, unlimited personas, 10GB storage
            SubscriptionTier.Basic => (200, 10, 1024, 30),     // 200 messages/day, 10 personas, 1GB, 30 days
            SubscriptionTier.Lifetime => (-1, -1, 102400, -1), // Same as Pro
            _ => (20, 3, 100, 7)                               // Free: 20 messages/day, 3 personas, 100MB, 7 days
        };
    }
}
