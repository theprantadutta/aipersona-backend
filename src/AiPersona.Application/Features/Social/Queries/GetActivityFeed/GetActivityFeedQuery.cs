using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.GetActivityFeed;

public record GetActivityFeedQuery(int Limit = 50, int Offset = 0) : IRequest<Result<ActivityFeedDto>>;

public class GetActivityFeedQueryHandler : IRequestHandler<GetActivityFeedQuery, Result<ActivityFeedDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetActivityFeedQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ActivityFeedDto>> Handle(GetActivityFeedQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ActivityFeedDto>.Failure("Unauthorized", 401);

        var query = _context.UserActivities
            .Where(a => a.UserId == _currentUser.UserId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var activities = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var dtos = activities.Select(a => new ActivityItemDto(
            a.Id,
            a.ActivityType.ToString(),
            a.Description ?? "",
            a.TargetId,
            a.TargetType?.ToString(),
            a.Metadata,
            a.CreatedAt)).ToList();

        return Result<ActivityFeedDto>.Success(new ActivityFeedDto(dtos, total, request.Limit, request.Offset));
    }
}
