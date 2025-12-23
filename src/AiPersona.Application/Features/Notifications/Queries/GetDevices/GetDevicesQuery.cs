using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Notifications.DTOs;

namespace AiPersona.Application.Features.Notifications.Queries.GetDevices;

public record GetDevicesQuery : IRequest<Result<DeviceListDto>>;

public class GetDevicesQueryHandler : IRequestHandler<GetDevicesQuery, Result<DeviceListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDevicesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<DeviceListDto>> Handle(GetDevicesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<DeviceListDto>.Failure("Unauthorized", 401);

        var devices = await _context.FcmTokens
            .Where(t => t.UserId == _currentUser.UserId)
            .OrderByDescending(t => t.LastActiveAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = devices.Select(d => new DeviceDto(
            d.Id,
            d.Token[..Math.Min(20, d.Token.Length)] + "...",
            d.Platform.ToString(),
            d.DeviceName,
            d.CreatedAt,
            d.LastActiveAt)).ToList();

        return Result<DeviceListDto>.Success(new DeviceListDto(dtos, devices.Count));
    }
}
