using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;

namespace AiPersona.Application.Features.Admin.Queries.GetUserDetails;

public record GetUserDetailsQuery(Guid UserId) : IRequest<Result<UserDetailDto>>;

public class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, Result<UserDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserDetailsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDetailDto>> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return Result<UserDetailDto>.Failure("User not found", 404);

        var personaCount = await _context.Personas
            .CountAsync(p => p.CreatorId == user.Id, cancellationToken);

        var sessionCount = await _context.ChatSessions
            .CountAsync(s => s.UserId == user.Id, cancellationToken);

        var messageCount = await _context.ChatMessages
            .CountAsync(m => m.Session.UserId == user.Id, cancellationToken);

        var usage = await _context.UsageTrackings
            .FirstOrDefaultAsync(u => u.UserId == user.Id, cancellationToken);

        return Result<UserDetailDto>.Success(new UserDetailDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.ProfileImage,
            user.AuthProvider.ToString(),
            user.SubscriptionTier.ToString(),
            user.IsActive,
            user.IsSuspended,
            user.SuspensionReason,
            user.SuspendedUntil,
            personaCount,
            sessionCount,
            messageCount,
            usage?.StorageUsedBytes ?? 0,
            usage?.GeminiTokensUsedTotal ?? 0,
            user.CreatedAt,
            user.LastLogin));
    }
}
