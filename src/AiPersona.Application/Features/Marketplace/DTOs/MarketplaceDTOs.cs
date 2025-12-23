namespace AiPersona.Application.Features.Marketplace.DTOs;

public record MarketplaceListingDto(
    Guid Id,
    Guid PersonaId,
    string PersonaName,
    string? PersonaImage,
    string Title,
    string Description,
    Guid SellerId,
    string SellerName,
    string PricingType,
    decimal Price,
    string Category,
    string Status,
    int ViewCount,
    int PurchaseCount,
    DateTime? ApprovedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record MarketplaceListingsDto(
    List<MarketplaceListingDto> Listings,
    int Total,
    int Page,
    int PageSize);

public record MarketplacePurchaseDto(
    Guid Id,
    Guid MarketplacePersonaId,
    Guid PersonaId,
    string PersonaName,
    string? PersonaImage,
    decimal Amount,
    string Status,
    DateTime PurchasedAt);

public record MarketplacePurchasesDto(
    List<MarketplacePurchaseDto> Purchases,
    int Total,
    int Page,
    int PageSize);

public record MarketplaceReviewDto(
    Guid Id,
    Guid MarketplacePersonaId,
    Guid ReviewerId,
    string ReviewerName,
    string? ReviewerImage,
    int Rating,
    string? ReviewText,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record MarketplaceReviewsDto(
    List<MarketplaceReviewDto> Reviews,
    int Total,
    int Page,
    int PageSize,
    double AverageRating);

public record ListingResultDto(
    Guid ListingId,
    string Status,
    string Message);

public record PurchaseResultDto(
    Guid PurchaseId,
    Guid PersonaId,
    string Status,
    string Message);

public record ReviewResultDto(
    Guid ReviewId,
    string Message);
