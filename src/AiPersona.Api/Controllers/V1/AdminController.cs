using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Admin.Commands.UpdateUser;
using AiPersona.Application.Features.Admin.Commands.SuspendUser;
using AiPersona.Application.Features.Admin.Commands.SuspendPersona;
using AiPersona.Application.Features.Admin.Commands.ResolveReport;
using AiPersona.Application.Features.Admin.Queries.GetUsers;
using AiPersona.Application.Features.Admin.Queries.GetUserDetails;
using AiPersona.Application.Features.Admin.Queries.GetDashboard;
using AiPersona.Application.Features.Admin.Queries.GetReports;

namespace AiPersona.Api.Controllers.V1;

[Authorize(Roles = "Admin")]
public class AdminController : BaseApiController
{
    /// <summary>
    /// Get admin dashboard analytics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard([FromQuery] int days = 30)
    {
        var result = await Mediator.Send(new GetDashboardQuery(days));
        return HandleResult(result);
    }

    /// <summary>
    /// Get all users with filters
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? subscriptionTier = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "created_at",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetUsersQuery(
            search, subscriptionTier, isActive, sortBy, sortOrder, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Get user details
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult> GetUserDetails(Guid userId)
    {
        var result = await Mediator.Send(new GetUserDetailsQuery(userId));
        return HandleResult(result);
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("users/{userId:guid}")]
    public async Task<ActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserCommand command)
    {
        command = command with { UserId = userId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Suspend or unsuspend a user
    /// </summary>
    [HttpPost("users/{userId:guid}/suspend")]
    public async Task<ActionResult> SuspendUser(Guid userId, [FromBody] SuspendUserCommand command)
    {
        command = command with { UserId = userId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Suspend or unsuspend a persona
    /// </summary>
    [HttpPost("personas/{personaId:guid}/suspend")]
    public async Task<ActionResult> SuspendPersona(Guid personaId, [FromBody] SuspendPersonaCommand command)
    {
        command = command with { PersonaId = personaId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get content reports
    /// </summary>
    [HttpGet("reports")]
    public async Task<ActionResult> GetReports(
        [FromQuery] string? status = null,
        [FromQuery] string? contentType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetReportsQuery(status, contentType, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Resolve a content report
    /// </summary>
    [HttpPost("reports/{reportId:guid}/resolve")]
    public async Task<ActionResult> ResolveReport(Guid reportId, [FromBody] ResolveReportCommand command)
    {
        command = command with { ReportId = reportId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
