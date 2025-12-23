using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Usage.Queries.GetCurrentUsage;
using AiPersona.Application.Features.Usage.Queries.GetUsageHistory;
using AiPersona.Application.Features.Usage.Queries.GetUsageAnalytics;

namespace AiPersona.Api.Controllers.V1;

public class UsageController : BaseApiController
{
    /// <summary>
    /// Get current usage statistics
    /// </summary>
    [HttpGet("current")]
    [Authorize]
    public async Task<ActionResult> GetCurrentUsage()
    {
        var result = await Mediator.Send(new GetCurrentUsageQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Get usage history
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult> GetUsageHistory(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        var result = await Mediator.Send(new GetUsageHistoryQuery(startDate, endDate, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Get usage analytics
    /// </summary>
    [HttpGet("analytics")]
    [Authorize]
    public async Task<ActionResult> GetUsageAnalytics([FromQuery] int days = 30)
    {
        var result = await Mediator.Send(new GetUsageAnalyticsQuery(days));
        return HandleResult(result);
    }
}
