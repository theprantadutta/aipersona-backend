using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Subscription.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Subscription.Queries.GetStatus;

public record GetStatusQuery : IRequest<Result<SubscriptionStatusDto>>;

public class GetStatusQueryHandler : IRequestHandler<GetStatusQuery, Result<SubscriptionStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetStatusQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<SubscriptionStatusDto>> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SubscriptionStatusDto>.Failure("Unauthorized", 401);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
            return Result<SubscriptionStatusDto>.Failure("User not found", 404);

        var isActive = user.SubscriptionTier != SubscriptionTier.Free &&
                       (user.SubscriptionExpiresAt == null || user.SubscriptionExpiresAt > _dateTime.UtcNow);

        var lastEvent = await _context.SubscriptionEvents
            .Where(e => e.UserId == _currentUser.UserId)
            .OrderByDescending(e => e.CreatedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return Result<SubscriptionStatusDto>.Success(new SubscriptionStatusDto(
            user.SubscriptionTier.ToString(),
            isActive,
            user.SubscriptionExpiresAt,
            lastEvent?.CreatedAt,
            lastEvent?.EventType != SubscriptionEventType.Cancelled,
            lastEvent?.ProductId,
            null));
    }
}
