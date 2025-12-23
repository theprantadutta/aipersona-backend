using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Marketplace.Commands.CreateReview;

public record CreateReviewCommand(
    Guid ListingId,
    int Rating,
    string? ReviewText = null) : IRequest<Result<ReviewResultDto>>;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<ReviewResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public CreateReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ReviewResultDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ReviewResultDto>.Failure("Unauthorized", 401);

        if (request.Rating < 1 || request.Rating > 5)
            return Result<ReviewResultDto>.Failure("Rating must be between 1 and 5", 400);

        var listing = await _context.MarketplacePersonas
            .FirstOrDefaultAsync(m => m.Id == request.ListingId, cancellationToken);

        if (listing == null)
            return Result<ReviewResultDto>.Failure("Listing not found", 404);

        // Check if user has purchased
        var hasPurchased = await _context.MarketplacePurchases
            .AnyAsync(p => p.MarketplacePersonaId == request.ListingId &&
                           p.BuyerId == _currentUser.UserId &&
                           p.Status == PurchaseStatus.Completed, cancellationToken);

        if (!hasPurchased)
            return Result<ReviewResultDto>.Failure("You must purchase before reviewing", 400);

        // Check for existing review
        var existingReview = await _context.MarketplaceReviews
            .FirstOrDefaultAsync(r => r.MarketplacePersonaId == request.ListingId && r.ReviewerId == _currentUser.UserId, cancellationToken);

        if (existingReview != null)
            return Result<ReviewResultDto>.Failure("Already reviewed", 400);

        var review = new MarketplaceReview
        {
            Id = Guid.NewGuid(),
            MarketplacePersonaId = request.ListingId,
            ReviewerId = _currentUser.UserId.Value,
            Rating = request.Rating,
            ReviewText = request.ReviewText,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };

        _context.MarketplaceReviews.Add(review);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ReviewResultDto>.Success(new ReviewResultDto(
            review.Id,
            "Review created successfully"));
    }
}
