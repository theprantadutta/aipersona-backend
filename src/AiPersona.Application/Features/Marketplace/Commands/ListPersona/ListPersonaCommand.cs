using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Marketplace.Commands.ListPersona;

public record ListPersonaCommand(
    Guid PersonaId,
    string Title,
    string Description,
    string Category,
    string PricingType,
    decimal Price = 0) : IRequest<Result<ListingResultDto>>;

public class ListPersonaCommandHandler : IRequestHandler<ListPersonaCommand, Result<ListingResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ListPersonaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ListingResultDto>> Handle(ListPersonaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ListingResultDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId && p.CreatorId == _currentUser.UserId, cancellationToken);

        if (persona == null)
            return Result<ListingResultDto>.Failure("Persona not found or you don't own it", 404);

        var existingListing = await _context.MarketplacePersonas
            .FirstOrDefaultAsync(m => m.PersonaId == request.PersonaId &&
                (m.Status == MarketplaceStatus.Pending || m.Status == MarketplaceStatus.Approved), cancellationToken);

        if (existingListing != null)
            return Result<ListingResultDto>.Failure("Persona is already listed", 400);

        if (!Enum.TryParse<PricingType>(request.PricingType, true, out var pricingType))
            return Result<ListingResultDto>.Failure("Invalid pricing type", 400);

        if (pricingType == PricingType.OneTime && request.Price <= 0)
            return Result<ListingResultDto>.Failure("Price is required for paid listings", 400);

        var listing = new MarketplacePersona
        {
            Id = Guid.NewGuid(),
            PersonaId = request.PersonaId,
            SellerId = _currentUser.UserId.Value,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            PricingType = pricingType,
            Price = pricingType == PricingType.OneTime ? request.Price : 0,
            Status = MarketplaceStatus.Pending,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };

        _context.MarketplacePersonas.Add(listing);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ListingResultDto>.Success(new ListingResultDto(
            listing.Id,
            listing.Status.ToString(),
            "Persona listed successfully, pending approval"));
    }
}
