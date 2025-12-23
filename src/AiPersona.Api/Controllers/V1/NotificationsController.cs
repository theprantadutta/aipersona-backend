using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Notifications.Commands.RegisterDevice;
using AiPersona.Application.Features.Notifications.Commands.UnregisterDevice;
using AiPersona.Application.Features.Notifications.Commands.SendNotification;
using AiPersona.Application.Features.Notifications.Queries.GetDevices;

namespace AiPersona.Api.Controllers.V1;

public class NotificationsController : BaseApiController
{
    /// <summary>
    /// Register device for push notifications
    /// </summary>
    [HttpPost("register")]
    [Authorize]
    public async Task<ActionResult> RegisterDevice([FromBody] RegisterDeviceCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Unregister device from push notifications
    /// </summary>
    [HttpDelete("unregister/{deviceToken}")]
    [Authorize]
    public async Task<ActionResult> UnregisterDevice(string deviceToken)
    {
        var result = await Mediator.Send(new UnregisterDeviceCommand(deviceToken));
        return HandleResult(result);
    }

    /// <summary>
    /// Get registered devices
    /// </summary>
    [HttpGet("devices")]
    [Authorize]
    public async Task<ActionResult> GetDevices()
    {
        var result = await Mediator.Send(new GetDevicesQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Send a test notification
    /// </summary>
    [HttpPost("test")]
    [Authorize]
    public async Task<ActionResult> SendTestNotification([FromBody] SendNotificationCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
