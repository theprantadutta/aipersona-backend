using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Subscription.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Subscription.Commands.VerifyPurchase;

public record VerifyPurchaseCommand(
    string ProductId,
    string PurchaseToken,
    string Platform) : IRequest<Result<VerifyPurchaseResultDto>>;

public class VerifyPurchaseCommandHandler : IRequestHandler<VerifyPurchaseCommand, Result<VerifyPurchaseResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IGooglePlayService _googlePlayService;
    private readonly IDateTimeService _dateTime;

    public VerifyPurchaseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IGooglePlayService googlePlayService,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _googlePlayService = googlePlayService;
        _dateTime = dateTime;
    }

    public async Task<Result<VerifyPurchaseResultDto>> Handle(VerifyPurchaseCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<VerifyPurchaseResultDto>.Failure("Unauthorized", 401);

        if (!Enum.TryParse<Platform>(request.Platform, true, out var platform))
            return Result<VerifyPurchaseResultDto>.Failure("Invalid platform", 400);

        // Verify with Google Play
        var verification = await _googlePlayService.VerifySubscriptionAsync(request.ProductId, request.PurchaseToken, cancellationToken);

        if (!verification.IsValid)
            return Result<VerifyPurchaseResultDto>.Success(new VerifyPurchaseResultDto(
                false, "Free", null, "Verification failed"));

        var tier = GetTierFromProductId(request.ProductId);
        var expiresAt = verification.ExpiresAt;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user != null)
        {
            user.SubscriptionTier = tier;
            user.SubscriptionExpiresAt = expiresAt;
            user.UpdatedAt = _dateTime.UtcNow;
        }

        var subscriptionEvent = new SubscriptionEvent
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            EventType = SubscriptionEventType.Purchased,
            SubscriptionTier = tier,
            ProductId = request.ProductId,
            PurchaseToken = request.PurchaseToken,
            ExpiresAt = expiresAt,
            CreatedAt = _dateTime.UtcNow
        };

        _context.SubscriptionEvents.Add(subscriptionEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<VerifyPurchaseResultDto>.Success(new VerifyPurchaseResultDto(
            true, tier.ToString(), expiresAt, "Subscription activated successfully"));
    }

    private static SubscriptionTier GetTierFromProductId(string productId)
    {
        return productId.ToLower() switch
        {
            var p when p.Contains("pro") => SubscriptionTier.Pro,
            var p when p.Contains("premium") => SubscriptionTier.Premium,
            var p when p.Contains("basic") => SubscriptionTier.Basic,
            _ => SubscriptionTier.Free
        };
    }
}
