using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Auth.DTOs;

namespace AiPersona.Application.Features.Auth.Queries.GetAuthProviders;

public record GetAuthProvidersQuery : IRequest<Result<AuthProvidersDto>>;

public class GetAuthProvidersQueryHandler : IRequestHandler<GetAuthProvidersQuery, Result<AuthProvidersDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAuthProvidersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AuthProvidersDto>> Handle(GetAuthProvidersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<AuthProvidersDto>.Failure("Unauthorized", 401);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
            return Result<AuthProvidersDto>.Failure("User not found", 404);

        var providers = new List<string>();
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
        var hasGoogle = !string.IsNullOrEmpty(user.FirebaseUid) || !string.IsNullOrEmpty(user.GoogleId);

        if (hasPassword)
            providers.Add("email");
        if (hasGoogle)
            providers.Add("google");

        return Result<AuthProvidersDto>.Success(new AuthProvidersDto(providers, hasPassword, hasGoogle));
    }
}
