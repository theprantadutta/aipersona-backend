using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Personas.Commands.CreatePersona;
using AiPersona.Application.Features.Personas.Commands.UpdatePersona;
using AiPersona.Application.Features.Personas.Commands.DeletePersona;
using AiPersona.Application.Features.Personas.Commands.ClonePersona;
using AiPersona.Application.Features.Personas.Commands.AddKnowledge;
using AiPersona.Application.Features.Personas.Commands.DeleteKnowledge;
using AiPersona.Application.Features.Personas.Queries.GetPersona;
using AiPersona.Application.Features.Personas.Queries.GetUserPersonas;
using AiPersona.Application.Features.Personas.Queries.GetPublicPersonas;
using AiPersona.Application.Features.Personas.Queries.GetTrendingPersonas;
using AiPersona.Application.Features.Personas.Queries.SearchPersonas;
using AiPersona.Application.Features.Personas.Queries.GetPersonaKnowledge;

namespace AiPersona.Api.Controllers.V1;

public class PersonasController : BaseApiController
{
    /// <summary>
    /// Get all personas created by the current user
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetUserPersonas(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetUserPersonasQuery(status, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new persona
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult> CreatePersona([FromBody] CreatePersonaCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get trending personas
    /// </summary>
    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<ActionResult> GetTrendingPersonas(
        [FromQuery] string timeframe = "week",
        [FromQuery] int limit = 20)
    {
        var result = await Mediator.Send(new GetTrendingPersonasQuery(timeframe, limit));
        return HandleResult(result);
    }

    /// <summary>
    /// Get all public personas
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPublicPersonas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetPublicPersonasQuery(page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Search public personas
    /// </summary>
    [HttpGet("search")]
    [Authorize]
    public async Task<ActionResult> SearchPersonas(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new SearchPersonasQuery(q, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific persona by ID
    /// </summary>
    [HttpGet("{personaId:guid}")]
    [Authorize]
    public async Task<ActionResult> GetPersona(Guid personaId)
    {
        var result = await Mediator.Send(new GetPersonaQuery(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Update a persona
    /// </summary>
    [HttpPut("{personaId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdatePersona(Guid personaId, [FromBody] UpdatePersonaCommand command)
    {
        command = command with { PersonaId = personaId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a persona (soft delete)
    /// </summary>
    [HttpDelete("{personaId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeletePersona(Guid personaId)
    {
        var result = await Mediator.Send(new DeletePersonaCommand(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Clone a persona
    /// </summary>
    [HttpPost("{personaId:guid}/clone")]
    [Authorize]
    public async Task<ActionResult> ClonePersona(Guid personaId, [FromBody] ClonePersonaCommand command)
    {
        command = command with { PersonaId = personaId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Add knowledge base entry to a persona
    /// </summary>
    [HttpPost("{personaId:guid}/knowledge")]
    [Authorize]
    public async Task<ActionResult> AddKnowledgeBase(Guid personaId, [FromBody] AddKnowledgeCommand command)
    {
        command = command with { PersonaId = personaId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all knowledge bases for a persona
    /// </summary>
    [HttpGet("{personaId:guid}/knowledge")]
    [Authorize]
    public async Task<ActionResult> GetPersonaKnowledgeBases(Guid personaId)
    {
        var result = await Mediator.Send(new GetPersonaKnowledgeQuery(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a knowledge base entry
    /// </summary>
    [HttpDelete("{personaId:guid}/knowledge/{knowledgeId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteKnowledgeBase(Guid personaId, Guid knowledgeId)
    {
        var result = await Mediator.Send(new DeleteKnowledgeCommand(personaId, knowledgeId));
        return HandleResult(result);
    }
}
