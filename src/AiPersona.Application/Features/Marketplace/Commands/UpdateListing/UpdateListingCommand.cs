using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Marketplace.Commands.UpdateListing;

public record UpdateListingCommand(
    Guid ListingId,
    string? Title = null,
    string? Description = null,
    string? Category = null,
    string? PricingType = null,
    decimal? Price = null) : IRequest<Result<ListingResultDto>>;

public class UpdateListingCommandHandler : IRequestHandler<UpdateListingCommand, Result<ListingResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public UpdateListingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ListingResultDto>> Handle(UpdateListingCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ListingResultDto>.Failure("Unauthorized", 401);

        var listing = await _context.MarketplacePersonas
            .FirstOrDefaultAsync(m => m.Id == request.ListingId && m.SellerId == _currentUser.UserId, cancellationToken);

        if (listing == null)
            return Result<ListingResultDto>.Failure("Listing not found or you don't own it", 404);

        if (request.Title != null)
            listing.Title = request.Title;

        if (request.Description != null)
            listing.Description = request.Description;

        if (request.Category != null)
            listing.Category = request.Category;

        if (request.PricingType != null && Enum.TryParse<PricingType>(request.PricingType, true, out var pricingType))
        {
            listing.PricingType = pricingType;
            if (pricingType == PricingType.Free)
                listing.Price = 0;
        }

        if (request.Price.HasValue)
            listing.Price = request.Price.Value;

        listing.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ListingResultDto>.Success(new ListingResultDto(
            listing.Id,
            listing.Status.ToString(),
            "Listing updated successfully"));
    }
}
