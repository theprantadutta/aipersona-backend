using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Marketplace.Commands.RemoveListing;

public record RemoveListingCommand(Guid ListingId) : IRequest<Result<ListingResultDto>>;

public class RemoveListingCommandHandler : IRequestHandler<RemoveListingCommand, Result<ListingResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public RemoveListingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ListingResultDto>> Handle(RemoveListingCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ListingResultDto>.Failure("Unauthorized", 401);

        var listing = await _context.MarketplacePersonas
            .FirstOrDefaultAsync(m => m.Id == request.ListingId && m.SellerId == _currentUser.UserId, cancellationToken);

        if (listing == null)
            return Result<ListingResultDto>.Failure("Listing not found or you don't own it", 404);

        // Remove the listing entirely from the database
        _context.MarketplacePersonas.Remove(listing);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ListingResultDto>.Success(new ListingResultDto(
            listing.Id,
            "Removed",
            "Listing removed successfully"));
    }
}
