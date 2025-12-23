using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AiPersona.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to persona updates
    /// </summary>
    public async Task SubscribeToPersona(Guid personaId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"persona_{personaId}");
        _logger.LogInformation("Connection {ConnectionId} subscribed to persona {PersonaId}", Context.ConnectionId, personaId);
    }

    /// <summary>
    /// Unsubscribe from persona updates
    /// </summary>
    public async Task UnsubscribeFromPersona(Guid personaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"persona_{personaId}");
        _logger.LogInformation("Connection {ConnectionId} unsubscribed from persona {PersonaId}", Context.ConnectionId, personaId);
    }
}

/// <summary>
/// Service for sending notifications via SignalR
/// </summary>
public interface INotificationService
{
    Task SendToUserAsync(Guid userId, string method, object payload);
    Task SendToPersonaSubscribersAsync(Guid personaId, string method, object payload);
}

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(Guid userId, string method, object payload)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync(method, payload);
    }

    public async Task SendToPersonaSubscribersAsync(Guid personaId, string method, object payload)
    {
        await _hubContext.Clients.Group($"persona_{personaId}").SendAsync(method, payload);
    }
}
