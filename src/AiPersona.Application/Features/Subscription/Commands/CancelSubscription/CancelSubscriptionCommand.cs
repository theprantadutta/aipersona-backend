using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Subscription.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Subscription.Commands.CancelSubscription;

public record CancelSubscriptionCommand : IRequest<Result<CancelSubscriptionResultDto>>;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result<CancelSubscriptionResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public CancelSubscriptionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<CancelSubscriptionResultDto>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<CancelSubscriptionResultDto>.Failure("Unauthorized", 401);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
            return Result<CancelSubscriptionResultDto>.Failure("User not found", 404);

        if (user.SubscriptionTier == SubscriptionTier.Free)
            return Result<CancelSubscriptionResultDto>.Success(new CancelSubscriptionResultDto(
                false, "No active subscription to cancel", null));

        var activeUntil = user.SubscriptionExpiresAt;

        var subscriptionEvent = new SubscriptionEvent
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            EventType = SubscriptionEventType.Cancelled,
            SubscriptionTier = user.SubscriptionTier,
            ExpiresAt = activeUntil ?? _dateTime.UtcNow,
            CreatedAt = _dateTime.UtcNow
        };

        _context.SubscriptionEvents.Add(subscriptionEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CancelSubscriptionResultDto>.Success(new CancelSubscriptionResultDto(
            true, "Subscription cancelled. Access continues until expiration.", activeUntil));
    }
}
