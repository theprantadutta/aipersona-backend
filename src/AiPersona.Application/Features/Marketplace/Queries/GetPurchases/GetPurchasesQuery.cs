using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Marketplace.DTOs;

namespace AiPersona.Application.Features.Marketplace.Queries.GetPurchases;

public record GetPurchasesQuery(int Page = 1, int PageSize = 20) : IRequest<Result<MarketplacePurchasesDto>>;

public class GetPurchasesQueryHandler : IRequestHandler<GetPurchasesQuery, Result<MarketplacePurchasesDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetPurchasesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<MarketplacePurchasesDto>> Handle(GetPurchasesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<MarketplacePurchasesDto>.Failure("Unauthorized", 401);

        var query = _context.MarketplacePurchases
            .Include(p => p.MarketplacePersona)
                .ThenInclude(l => l.Persona)
            .Where(p => p.BuyerId == _currentUser.UserId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var purchases = await query
            .OrderByDescending(p => p.PurchasedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = purchases.Select(p => new MarketplacePurchaseDto(
            p.Id,
            p.MarketplacePersonaId,
            p.MarketplacePersona?.PersonaId ?? Guid.Empty,
            p.MarketplacePersona?.Persona?.Name ?? "Unknown",
            p.MarketplacePersona?.Persona?.ImagePath,
            p.Amount,
            p.Status.ToString(),
            p.PurchasedAt)).ToList();

        return Result<MarketplacePurchasesDto>.Success(new MarketplacePurchasesDto(dtos, total, request.Page, request.PageSize));
    }
}
