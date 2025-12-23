namespace AiPersona.Application.Features.Notifications.DTOs;

public record DeviceDto(
    Guid Id,
    string Token,
    string Platform,
    string? DeviceName,
    DateTime RegisteredAt,
    DateTime? LastActiveAt);

public record DeviceListDto(
    List<DeviceDto> Devices,
    int Total);

public record RegisterDeviceResultDto(
    Guid DeviceId,
    bool IsNew,
    string Message);

public record UnregisterDeviceResultDto(
    bool Success,
    string Message);

public record SendNotificationResultDto(
    bool Success,
    int DevicesSent,
    string Message);
