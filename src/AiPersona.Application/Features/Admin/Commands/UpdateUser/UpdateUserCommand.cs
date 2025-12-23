using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Admin.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid UserId,
    string? DisplayName = null,
    string? SubscriptionTier = null,
    bool? IsAdmin = null) : IRequest<Result<UpdateUserResultDto>>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UpdateUserResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public UpdateUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<UpdateUserResultDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<UpdateUserResultDto>.Failure("Admin access required", 403);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return Result<UpdateUserResultDto>.Failure("User not found", 404);

        if (request.DisplayName != null)
            user.DisplayName = request.DisplayName;

        if (request.SubscriptionTier != null && Enum.TryParse<SubscriptionTier>(request.SubscriptionTier, true, out var tier))
            user.SubscriptionTier = tier;

        if (request.IsAdmin.HasValue)
            user.IsAdmin = request.IsAdmin.Value;

        user.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<UpdateUserResultDto>.Success(new UpdateUserResultDto(true, "User updated successfully"));
    }
}
