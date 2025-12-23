using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;

namespace AiPersona.Application.Features.Marketplace.Queries.GetListing;

public record GetListingQuery(Guid ListingId) : IRequest<Result<MarketplaceListingDto>>;

public class GetListingQueryHandler : IRequestHandler<GetListingQuery, Result<MarketplaceListingDto>>
{
    private readonly IApplicationDbContext _context;

    public GetListingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MarketplaceListingDto>> Handle(GetListingQuery request, CancellationToken cancellationToken)
    {
        var listing = await _context.MarketplacePersonas
            .Include(m => m.Persona)
            .Include(m => m.Seller)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.ListingId, cancellationToken);

        if (listing == null)
            return Result<MarketplaceListingDto>.Failure("Listing not found", 404);

        var dto = new MarketplaceListingDto(
            listing.Id,
            listing.PersonaId,
            listing.Persona?.Name ?? "Unknown",
            listing.Persona?.ImagePath,
            listing.Title,
            listing.Description,
            listing.SellerId,
            listing.Seller?.DisplayName ?? listing.Seller?.Email ?? "Unknown",
            listing.PricingType.ToString(),
            listing.Price,
            listing.Category,
            listing.Status.ToString(),
            listing.ViewCount,
            listing.PurchaseCount,
            listing.ApprovedAt,
            listing.CreatedAt,
            listing.UpdatedAt);

        return Result<MarketplaceListingDto>.Success(dto);
    }
}
