using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;

namespace AiPersona.Application.Features.Marketplace.Queries.GetReviews;

public record GetReviewsQuery(Guid ListingId, int Page = 1, int PageSize = 20) : IRequest<Result<MarketplaceReviewsDto>>;

public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, Result<MarketplaceReviewsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReviewsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MarketplaceReviewsDto>> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        var listing = await _context.MarketplacePersonas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.ListingId, cancellationToken);

        if (listing == null)
            return Result<MarketplaceReviewsDto>.Failure("Listing not found", 404);

        var query = _context.MarketplaceReviews
            .Include(r => r.Reviewer)
            .Where(r => r.MarketplacePersonaId == request.ListingId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Calculate average rating from reviews
        var avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0.0;

        var dtos = reviews.Select(r => new MarketplaceReviewDto(
            r.Id,
            r.MarketplacePersonaId,
            r.ReviewerId,
            r.Reviewer?.DisplayName ?? r.Reviewer?.Email ?? "Unknown",
            r.Reviewer?.ProfileImage,
            r.Rating,
            r.ReviewText,
            r.CreatedAt,
            r.UpdatedAt)).ToList();

        return Result<MarketplaceReviewsDto>.Success(new MarketplaceReviewsDto(
            dtos, total, request.Page, request.PageSize, avgRating));
    }
}
