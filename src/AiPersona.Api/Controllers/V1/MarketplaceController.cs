using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Marketplace.Commands.ListPersona;
using AiPersona.Application.Features.Marketplace.Commands.UpdateListing;
using AiPersona.Application.Features.Marketplace.Commands.RemoveListing;
using AiPersona.Application.Features.Marketplace.Commands.PurchasePersona;
using AiPersona.Application.Features.Marketplace.Commands.CreateReview;
using AiPersona.Application.Features.Marketplace.Queries.GetListings;
using AiPersona.Application.Features.Marketplace.Queries.GetListing;
using AiPersona.Application.Features.Marketplace.Queries.GetPurchases;
using AiPersona.Application.Features.Marketplace.Queries.GetReviews;

namespace AiPersona.Api.Controllers.V1;

public class MarketplaceController : BaseApiController
{
    /// <summary>
    /// Get marketplace listings
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetListings(
        [FromQuery] string? category = null,
        [FromQuery] string? pricingType = null,
        [FromQuery] string sortBy = "created_at",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetListingsQuery(
            category, pricingType, sortBy, sortOrder, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific marketplace listing
    /// </summary>
    [HttpGet("{listingId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetListing(Guid listingId)
    {
        var result = await Mediator.Send(new GetListingQuery(listingId));
        return HandleResult(result);
    }

    /// <summary>
    /// List a persona in the marketplace
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult> ListPersona([FromBody] ListPersonaCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update a marketplace listing
    /// </summary>
    [HttpPut("{listingId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateListing(Guid listingId, [FromBody] UpdateListingCommand command)
    {
        command = command with { ListingId = listingId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove a listing from marketplace
    /// </summary>
    [HttpDelete("{listingId:guid}")]
    [Authorize]
    public async Task<ActionResult> RemoveListing(Guid listingId)
    {
        var result = await Mediator.Send(new RemoveListingCommand(listingId));
        return HandleResult(result);
    }

    /// <summary>
    /// Purchase a persona from marketplace
    /// </summary>
    [HttpPost("{listingId:guid}/purchase")]
    [Authorize]
    public async Task<ActionResult> PurchasePersona(Guid listingId, [FromBody] PurchasePersonaCommand command)
    {
        command = command with { ListingId = listingId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user's marketplace purchases
    /// </summary>
    [HttpGet("purchases")]
    [Authorize]
    public async Task<ActionResult> GetPurchases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPurchasesQuery(page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Get reviews for a listing
    /// </summary>
    [HttpGet("{listingId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult> GetReviews(
        Guid listingId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetReviewsQuery(listingId, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a review for a purchased persona
    /// </summary>
    [HttpPost("{listingId:guid}/reviews")]
    [Authorize]
    public async Task<ActionResult> CreateReview(Guid listingId, [FromBody] CreateReviewCommand command)
    {
        command = command with { ListingId = listingId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
