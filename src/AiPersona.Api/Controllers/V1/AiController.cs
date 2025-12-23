using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Ai.Commands.GenerateResponse;
using AiPersona.Application.Features.Ai.Commands.StreamResponse;
using AiPersona.Application.Features.Ai.Commands.AnalyzeSentiment;

namespace AiPersona.Api.Controllers.V1;

public class AiController : BaseApiController
{
    /// <summary>
    /// Generate AI response using Gemini
    /// </summary>
    [HttpPost("generate")]
    [Authorize]
    public async Task<ActionResult> GenerateResponse([FromBody] GenerateResponseCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Generate streaming AI response using Gemini (Server-Sent Events)
    /// </summary>
    [HttpPost("stream")]
    [Authorize]
    public async Task<ActionResult> StreamResponse([FromBody] StreamResponseCommand command)
    {
        // Note: Actual streaming will be handled by SignalR ChatHub
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Analyze sentiment of text
    /// </summary>
    [HttpPost("sentiment")]
    [Authorize]
    public async Task<ActionResult> AnalyzeSentiment([FromBody] AnalyzeSentimentCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
