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

        var usage = await _context.UsageTrackings
            .FirstOrDefaultAsync(u => u.UserId == _currentUser.UserId, cancellationToken);

        var today = _dateTime.UtcNow.Date;
        var messagesToday = usage?.MessagesToday ?? 0;
        if (usage?.MessagesCountResetAt.Date != today)
            messagesToday = 0;

        var personaCount = await _context.Personas
            .Where(p => p.CreatorId == _currentUser.UserId && p.Status != PersonaStatus.Archived)
            .CountAsync(cancellationToken);

        var storageUsed = await _context.UploadedFiles
            .Where(f => f.UserId == _currentUser.UserId)
            .SumAsync(f => f.FileSize, cancellationToken);

        var (messageLimit, personaLimit, storageLimitMb, historyDays) = GetLimitsForTier(user.SubscriptionTier);

        var nextReset = today.AddDays(1);

        return Result<CurrentUsageDto>.Success(new CurrentUsageDto(
            messagesToday,
            messageLimit,
            personaCount,
            personaLimit,
            storageUsed,
            storageLimitMb * 1024 * 1024,
            historyDays,
            historyDays,
            user.SubscriptionTier.ToString(),
            nextReset));
    }

    private static (int messageLimit, int personaLimit, int storageMb, int historyDays) GetLimitsForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Pro => (-1, -1, 102400, -1),
            SubscriptionTier.Premium => (-1, -1, 10240, -1),
            SubscriptionTier.Basic => (500, 10, 1024, 30),
            _ => (50, 3, 100, 7)
        };
    }
}
