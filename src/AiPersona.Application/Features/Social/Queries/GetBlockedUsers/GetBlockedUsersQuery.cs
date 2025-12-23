using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Queries.GetBlockedUsers;

public record GetBlockedUsersQuery(int Limit = 50, int Offset = 0) : IRequest<Result<BlockedUsersListDto>>;

public class GetBlockedUsersQueryHandler : IRequestHandler<GetBlockedUsersQuery, Result<BlockedUsersListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBlockedUsersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<BlockedUsersListDto>> Handle(GetBlockedUsersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<BlockedUsersListDto>.Failure("Unauthorized", 401);

        var query = _context.UserBlocks
            .Include(b => b.Blocked)
            .Where(b => b.BlockerId == _currentUser.UserId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var blocks = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var dtos = blocks.Select(b => new BlockedUserDto(
            b.BlockedId,
            b.Blocked?.DisplayName ?? b.Blocked?.Email ?? "Unknown",
            b.Blocked?.ProfileImage,
            b.Reason,
            b.CreatedAt)).ToList();

        return Result<BlockedUsersListDto>.Success(new BlockedUsersListDto(dtos, total, request.Limit, request.Offset));
    }
}
