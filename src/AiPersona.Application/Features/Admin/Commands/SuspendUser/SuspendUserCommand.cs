using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;

namespace AiPersona.Application.Features.Admin.Commands.SuspendUser;

public record SuspendUserCommand(
    Guid UserId,
    string? Reason = null,
    int? DurationDays = null) : IRequest<Result<SuspendResultDto>>;

public class SuspendUserCommandHandler : IRequestHandler<SuspendUserCommand, Result<SuspendResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public SuspendUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<SuspendResultDto>> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<SuspendResultDto>.Failure("Admin access required", 403);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return Result<SuspendResultDto>.Failure("User not found", 404);

        if (user.IsAdmin)
            return Result<SuspendResultDto>.Failure("Cannot suspend admin users", 400);

        user.IsSuspended = true;
        user.SuspendedAt = _dateTime.UtcNow;
        user.SuspendedUntil = request.DurationDays.HasValue
            ? _dateTime.UtcNow.AddDays(request.DurationDays.Value)
            : null;
        user.SuspensionReason = request.Reason;
        user.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SuspendResultDto>.Success(new SuspendResultDto(
            true, "User suspended", user.SuspendedUntil));
    }
}
