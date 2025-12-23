using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Support.Commands.CreateTicket;
using AiPersona.Application.Features.Support.Commands.AddMessage;
using AiPersona.Application.Features.Support.Commands.CloseTicket;
using AiPersona.Application.Features.Support.Commands.ReopenTicket;
using AiPersona.Application.Features.Support.Commands.UpdateTicket;
using AiPersona.Application.Features.Support.Commands.AssignTicket;
using AiPersona.Application.Features.Support.Commands.EscalateTicket;
using AiPersona.Application.Features.Support.Queries.GetTicket;
using AiPersona.Application.Features.Support.Queries.GetTickets;
using AiPersona.Application.Features.Support.Queries.GetTicketMessages;

namespace AiPersona.Api.Controllers.V1;

public class SupportController : BaseApiController
{
    /// <summary>
    /// Create a new support ticket
    /// </summary>
    [HttpPost("tickets")]
    [Authorize]
    public async Task<ActionResult> CreateTicket([FromBody] CreateTicketCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user's support tickets
    /// </summary>
    [HttpGet("tickets")]
    [Authorize]
    public async Task<ActionResult> GetTickets(
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetTicketsQuery(status, priority, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific support ticket
    /// </summary>
    [HttpGet("tickets/{ticketId:guid}")]
    [Authorize]
    public async Task<ActionResult> GetTicket(Guid ticketId)
    {
        var result = await Mediator.Send(new GetTicketQuery(ticketId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get messages for a ticket
    /// </summary>
    [HttpGet("tickets/{ticketId:guid}/messages")]
    [Authorize]
    public async Task<ActionResult> GetTicketMessages(
        Guid ticketId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetTicketMessagesQuery(ticketId, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Add a message to a ticket
    /// </summary>
    [HttpPost("tickets/{ticketId:guid}/messages")]
    [Authorize]
    public async Task<ActionResult> AddMessage(Guid ticketId, [FromBody] AddMessageCommand command)
    {
        command = command with { TicketId = ticketId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Close a support ticket
    /// </summary>
    [HttpPost("tickets/{ticketId:guid}/close")]
    [Authorize]
    public async Task<ActionResult> CloseTicket(Guid ticketId)
    {
        var result = await Mediator.Send(new CloseTicketCommand(ticketId));
        return HandleResult(result);
    }

    /// <summary>
    /// Reopen a closed support ticket
    /// </summary>
    [HttpPost("tickets/{ticketId:guid}/reopen")]
    [Authorize]
    public async Task<ActionResult> ReopenTicket(Guid ticketId)
    {
        var result = await Mediator.Send(new ReopenTicketCommand(ticketId));
        return HandleResult(result);
    }

    /// <summary>
    /// Update a support ticket (admin only)
    /// </summary>
    [HttpPut("tickets/{ticketId:guid}")]
    [Authorize(Roles = "Admin,Support")]
    public async Task<ActionResult> UpdateTicket(Guid ticketId, [FromBody] UpdateTicketCommand command)
    {
        command = command with { TicketId = ticketId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Assign a ticket to support agent (admin only)
    /// </summary>
    [HttpPost("tickets/{ticketId:guid}/assign")]
    [Authorize(Roles = "Admin,Support")]
    public async Task<ActionResult> AssignTicket(Guid ticketId, [FromBody] AssignTicketCommand command)
    {
        command = command with { TicketId = ticketId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Escalate a ticket priority (admin only)
    /// </summary>
    [HttpPost("tickets/{ticketId:guid}/escalate")]
    [Authorize(Roles = "Admin,Support")]
    public async Task<ActionResult> EscalateTicket(Guid ticketId)
    {
        var result = await Mediator.Send(new EscalateTicketCommand(ticketId));
        return HandleResult(result);
    }
}
