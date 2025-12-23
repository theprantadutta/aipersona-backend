using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Notifications.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Notifications.Commands.RegisterDevice;

public record RegisterDeviceCommand(
    string Token,
    string Platform,
    string? DeviceName = null) : IRequest<Result<RegisterDeviceResultDto>>;

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result<RegisterDeviceResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public RegisterDeviceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<RegisterDeviceResultDto>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<RegisterDeviceResultDto>.Failure("Unauthorized", 401);

        if (!Enum.TryParse<Platform>(request.Platform, true, out var platform))
            return Result<RegisterDeviceResultDto>.Failure("Invalid platform", 400);

        var existingToken = await _context.FcmTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (existingToken != null)
        {
            if (existingToken.UserId != _currentUser.UserId)
            {
                existingToken.UserId = _currentUser.UserId.Value;
            }
            existingToken.Platform = platform;
            existingToken.DeviceName = request.DeviceName;
            existingToken.LastActiveAt = _dateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result<RegisterDeviceResultDto>.Success(new RegisterDeviceResultDto(
                existingToken.Id, false, "Device updated"));
        }

        var device = new FcmToken
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            Token = request.Token,
            Platform = platform,
            DeviceName = request.DeviceName,
            CreatedAt = _dateTime.UtcNow,
            LastActiveAt = _dateTime.UtcNow
        };

        _context.FcmTokens.Add(device);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<RegisterDeviceResultDto>.Success(new RegisterDeviceResultDto(
            device.Id, true, "Device registered"));
    }
}
