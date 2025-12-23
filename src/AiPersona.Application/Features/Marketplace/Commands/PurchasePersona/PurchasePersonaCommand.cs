using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Marketplace.Commands.PurchasePersona;

public record PurchasePersonaCommand(
    Guid ListingId,
    string? PaymentToken = null) : IRequest<Result<PurchaseResultDto>>;

public class PurchasePersonaCommandHandler : IRequestHandler<PurchasePersonaCommand, Result<PurchaseResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public PurchasePersonaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<PurchaseResultDto>> Handle(PurchasePersonaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<PurchaseResultDto>.Failure("Unauthorized", 401);

        var listing = await _context.MarketplacePersonas
            .Include(m => m.Persona)
            .FirstOrDefaultAsync(m => m.Id == request.ListingId && m.Status == MarketplaceStatus.Approved, cancellationToken);

        if (listing == null)
            return Result<PurchaseResultDto>.Failure("Listing not found or not available", 404);

        if (listing.SellerId == _currentUser.UserId)
            return Result<PurchaseResultDto>.Failure("Cannot purchase your own listing", 400);

        var existingPurchase = await _context.MarketplacePurchases
            .FirstOrDefaultAsync(p => p.MarketplacePersonaId == request.ListingId &&
                                       p.BuyerId == _currentUser.UserId &&
                                       p.Status == PurchaseStatus.Completed, cancellationToken);

        if (existingPurchase != null)
            return Result<PurchaseResultDto>.Failure("Already purchased", 400);

        // For paid listings, validate payment (simplified - real implementation would verify with payment provider)
        if (listing.PricingType == PricingType.OneTime)
        {
            if (string.IsNullOrEmpty(request.PaymentToken))
                return Result<PurchaseResultDto>.Failure("Payment required", 400);
        }

        var purchase = new MarketplacePurchase
        {
            Id = Guid.NewGuid(),
            MarketplacePersonaId = listing.Id,
            BuyerId = _currentUser.UserId.Value,
            Amount = listing.Price,
            Status = PurchaseStatus.Completed,
            PurchasedAt = _dateTime.UtcNow
        };

        _context.MarketplacePurchases.Add(purchase);
        listing.PurchaseCount++;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PurchaseResultDto>.Success(new PurchaseResultDto(
            purchase.Id,
            listing.PersonaId,
            purchase.Status.ToString(),
            "Purchase completed successfully"));
    }
}
