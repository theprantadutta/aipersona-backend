using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Subscription.DTOs;

namespace AiPersona.Application.Features.Subscription.Queries.GetHistory;

public record GetHistoryQuery(int Page = 1, int PageSize = 20) : IRequest<Result<SubscriptionHistoryDto>>;

public class GetHistoryQueryHandler : IRequestHandler<GetHistoryQuery, Result<SubscriptionHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<SubscriptionHistoryDto>> Handle(GetHistoryQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SubscriptionHistoryDto>.Failure("Unauthorized", 401);

        var query = _context.SubscriptionEvents
            .Where(e => e.UserId == _currentUser.UserId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = events.Select(e => new SubscriptionEventDto(
            e.Id,
            e.EventType.ToString(),
            e.SubscriptionTier.ToString(),
            e.ProductId,
            null,
            e.PurchaseToken,
            e.CreatedAt)).ToList();

        return Result<SubscriptionHistoryDto>.Success(new SubscriptionHistoryDto(
            dtos, total, request.Page, request.PageSize));
    }
}
