using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;

namespace AiPersona.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<Result<UserDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UserDto>.Failure("Unauthorized", 401);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
            return Result<UserDto>.Failure("User not found", 404);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.PhotoUrl,
            user.SubscriptionTier.ToString(),
            user.IsActive,
            user.EmailVerified,
            user.AuthProvider.ToString(),
            user.CreatedAt,
            user.LastLogin));
    }
}
