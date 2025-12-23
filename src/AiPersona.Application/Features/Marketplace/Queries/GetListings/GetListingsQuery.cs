using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Marketplace.Queries.GetListings;

public record GetListingsQuery(
    string? Category = null,
    string? PricingType = null,
    string SortBy = "created_at",
    string SortOrder = "desc",
    int Page = 1,
    int PageSize = 20) : IRequest<Result<MarketplaceListingsDto>>;

public class GetListingsQueryHandler : IRequestHandler<GetListingsQuery, Result<MarketplaceListingsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetListingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MarketplaceListingsDto>> Handle(GetListingsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MarketplacePersonas
            .Include(m => m.Persona)
            .Include(m => m.Seller)
            .Where(m => m.Status == MarketplaceStatus.Approved)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(m => m.Category == request.Category);

        if (!string.IsNullOrEmpty(request.PricingType) && Enum.TryParse<PricingType>(request.PricingType, true, out var pricingType))
            query = query.Where(m => m.PricingType == pricingType);

        query = (request.SortBy.ToLower(), request.SortOrder.ToLower()) switch
        {
            ("price", "asc") => query.OrderBy(m => m.Price),
            ("price", _) => query.OrderByDescending(m => m.Price),
            ("purchases", "asc") => query.OrderBy(m => m.PurchaseCount),
            ("purchases", _) => query.OrderByDescending(m => m.PurchaseCount),
            ("views", "asc") => query.OrderBy(m => m.ViewCount),
            ("views", _) => query.OrderByDescending(m => m.ViewCount),
            (_, "asc") => query.OrderBy(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var listings = await query.Skip(skip).Take(request.PageSize).ToListAsync(cancellationToken);

        var dtos = listings.Select(m => new MarketplaceListingDto(
            m.Id,
            m.PersonaId,
            m.Persona?.Name ?? "Unknown",
            m.Persona?.ImagePath,
            m.Title,
            m.Description,
            m.SellerId,
            m.Seller?.DisplayName ?? m.Seller?.Email ?? "Unknown",
            m.PricingType.ToString(),
            m.Price,
            m.Category,
            m.Status.ToString(),
            m.ViewCount,
            m.PurchaseCount,
            m.ApprovedAt,
            m.CreatedAt,
            m.UpdatedAt)).ToList();

        return Result<MarketplaceListingsDto>.Success(new MarketplaceListingsDto(dtos, total, request.Page, request.PageSize));
    }
}
