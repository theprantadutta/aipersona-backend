using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;

namespace AiPersona.Application.Features.Admin.Queries.GetUsers;

public record GetUsersQuery(
    string? Search = null,
    string? Tier = null,
    bool? IsSuspended = null,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<AdminUserListDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<AdminUserListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUsersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminUserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<AdminUserListDto>.Failure("Admin access required", 403);

        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) ||
                                     (u.DisplayName != null && u.DisplayName.ToLower().Contains(search)));
        }

        if (request.IsSuspended.HasValue)
            query = query.Where(u => u.IsSuspended == request.IsSuspended);

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var personaCounts = await _context.Personas
            .Where(p => userIds.Contains(p.CreatorId))
            .GroupBy(p => p.CreatorId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var dtos = users.Select(u => new AdminUserDto(
            u.Id,
            u.Email,
            u.DisplayName,
            u.ProfileImage,
            u.SubscriptionTier.ToString(),
            u.IsAdmin,
            u.IsSuspended,
            u.SuspendedUntil,
            personaCounts.TryGetValue(u.Id, out var pc) ? pc : 0,
            0,
            u.CreatedAt,
            u.LastActiveAt)).ToList();

        return Result<AdminUserListDto>.Success(new AdminUserListDto(dtos, total, request.Page, request.PageSize));
    }
}
