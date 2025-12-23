using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Subscription.Commands.VerifyPurchase;
using AiPersona.Application.Features.Subscription.Commands.CancelSubscription;
using AiPersona.Application.Features.Subscription.Queries.GetStatus;
using AiPersona.Application.Features.Subscription.Queries.GetHistory;
using AiPersona.Application.Features.Subscription.Queries.GetPlans;

namespace AiPersona.Api.Controllers.V1;

public class SubscriptionController : BaseApiController
{
    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPlans()
    {
        var result = await Mediator.Send(new GetPlansQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Get current subscription status
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult> GetStatus()
    {
        var result = await Mediator.Send(new GetStatusQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Verify a Google Play purchase
    /// </summary>
    [HttpPost("verify")]
    [Authorize]
    public async Task<ActionResult> VerifyPurchase([FromBody] VerifyPurchaseCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get subscription history
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetHistoryQuery(page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost("cancel")]
    [Authorize]
    public async Task<ActionResult> CancelSubscription()
    {
        var result = await Mediator.Send(new CancelSubscriptionCommand());
        return HandleResult(result);
    }
}
