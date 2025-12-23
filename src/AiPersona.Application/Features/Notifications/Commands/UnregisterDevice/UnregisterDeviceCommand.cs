using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Notifications.DTOs;

namespace AiPersona.Application.Features.Notifications.Commands.UnregisterDevice;

public record UnregisterDeviceCommand(string Token) : IRequest<Result<UnregisterDeviceResultDto>>;

public class UnregisterDeviceCommandHandler : IRequestHandler<UnregisterDeviceCommand, Result<UnregisterDeviceResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UnregisterDeviceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UnregisterDeviceResultDto>> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UnregisterDeviceResultDto>.Failure("Unauthorized", 401);

        var device = await _context.FcmTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token && t.UserId == _currentUser.UserId, cancellationToken);

        if (device == null)
            return Result<UnregisterDeviceResultDto>.Success(new UnregisterDeviceResultDto(
                false, "Device not found"));

        _context.FcmTokens.Remove(device);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<UnregisterDeviceResultDto>.Success(new UnregisterDeviceResultDto(
            true, "Device unregistered"));
    }
}
