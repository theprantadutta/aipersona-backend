using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using AiPersona.Application.Features.Chat.Commands.SendMessage;

namespace AiPersona.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ISender _mediator;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ISender mediator, ILogger<ChatHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a chat session room
    /// </summary>
    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        _logger.LogInformation("Connection {ConnectionId} joined session {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Leave a chat session room
    /// </summary>
    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        _logger.LogInformation("Connection {ConnectionId} left session {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Send a message and receive streaming AI response
    /// </summary>
    public async Task SendMessageStream(Guid sessionId, string message, double? temperature = null)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        try
        {
            // Notify typing indicator
            await Clients.Group($"session_{sessionId}").SendAsync("TypingStart", sessionId);

            // Send message via MediatR
            var command = new SendMessageCommand(sessionId, message, temperature);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // Send user message
                await Clients.Group($"session_{sessionId}").SendAsync("UserMessage", result.Value?.UserMessage);

                // Send AI response
                await Clients.Group($"session_{sessionId}").SendAsync("AiMessage", result.Value?.AiMessage);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Error);
            }

            // Stop typing indicator
            await Clients.Group($"session_{sessionId}").SendAsync("TypingStop", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message in session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "Failed to send message");
            await Clients.Group($"session_{sessionId}").SendAsync("TypingStop", sessionId);
        }
    }

    /// <summary>
    /// Send typing indicator
    /// </summary>
    public async Task Typing(Guid sessionId, bool isTyping)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.OthersInGroup($"session_{sessionId}").SendAsync(
            isTyping ? "UserTypingStart" : "UserTypingStop",
            sessionId,
            userId);
    }
}
