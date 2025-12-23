using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Chat.Commands.CreateSession;
using AiPersona.Application.Features.Chat.Commands.DeleteSession;
using AiPersona.Application.Features.Chat.Commands.UpdateSession;
using AiPersona.Application.Features.Chat.Commands.TogglePin;
using AiPersona.Application.Features.Chat.Commands.SendMessage;
using AiPersona.Application.Features.Chat.Commands.ExportSession;
using AiPersona.Application.Features.Chat.Queries.GetSession;
using AiPersona.Application.Features.Chat.Queries.GetSessions;
using AiPersona.Application.Features.Chat.Queries.SearchSessions;
using AiPersona.Application.Features.Chat.Queries.GetMessages;
using AiPersona.Application.Features.Chat.Queries.GetStatistics;

namespace AiPersona.Api.Controllers.V1;

public class ChatController : BaseApiController
{
    /// <summary>
    /// Get all chat sessions for the current user
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult> GetChatSessions(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetSessionsQuery(status, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    [HttpPost("sessions")]
    [Authorize]
    public async Task<ActionResult> CreateChatSession([FromBody] CreateSessionCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Search chat sessions with advanced filtering
    /// </summary>
    [HttpGet("sessions/search")]
    [Authorize]
    public async Task<ActionResult> SearchSessions(
        [FromQuery] string? q = null,
        [FromQuery] Guid? personaId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isPinned = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string sortBy = "last_message_at",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new SearchSessionsQuery(
            q, personaId, status, isPinned, startDate, endDate,
            sortBy, sortOrder, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific chat session by ID
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<ActionResult> GetChatSession(
        Guid sessionId,
        [FromQuery] bool includeMessages = true,
        [FromQuery] int messagesLimit = 100)
    {
        var result = await Mediator.Send(new GetSessionQuery(sessionId, includeMessages, messagesLimit));
        return HandleResult(result);
    }

    /// <summary>
    /// Update a chat session
    /// </summary>
    [HttpPut("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateChatSession(Guid sessionId, [FromBody] UpdateSessionCommand command)
    {
        command = command with { SessionId = sessionId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a chat session (soft delete)
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteChatSession(Guid sessionId)
    {
        var result = await Mediator.Send(new DeleteSessionCommand(sessionId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get messages from a chat session
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    [Authorize]
    public async Task<ActionResult> GetSessionMessages(
        Guid sessionId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var result = await Mediator.Send(new GetMessagesQuery(sessionId, skip, limit));
        return HandleResult(result);
    }

    /// <summary>
    /// Send a message in a chat session and get AI response
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/messages")]
    [Authorize]
    public async Task<ActionResult> SendMessage(Guid sessionId, [FromBody] SendMessageCommand command)
    {
        command = command with { SessionId = sessionId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Export a chat session
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/export")]
    [Authorize]
    public async Task<ActionResult> ExportChatSession(Guid sessionId, [FromBody] ExportSessionCommand command)
    {
        command = command with { SessionId = sessionId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Toggle pin status of a chat session
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/pin")]
    [Authorize]
    public async Task<ActionResult> ToggleSessionPin(Guid sessionId)
    {
        var result = await Mediator.Send(new TogglePinCommand(sessionId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get chat activity statistics
    /// </summary>
    [HttpGet("statistics")]
    [Authorize]
    public async Task<ActionResult> GetChatStatistics()
    {
        var result = await Mediator.Send(new GetStatisticsQuery());
        return HandleResult(result);
    }
}
