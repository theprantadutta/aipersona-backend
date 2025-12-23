using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Social.Commands.ToggleLike;
using AiPersona.Application.Features.Social.Commands.ToggleFavorite;
using AiPersona.Application.Features.Social.Commands.ToggleFollow;
using AiPersona.Application.Features.Social.Commands.BlockUser;
using AiPersona.Application.Features.Social.Commands.UnblockUser;
using AiPersona.Application.Features.Social.Commands.ReportContent;
using AiPersona.Application.Features.Social.Queries.CheckLiked;
using AiPersona.Application.Features.Social.Queries.CheckFavorited;
using AiPersona.Application.Features.Social.Queries.GetFavorites;
using AiPersona.Application.Features.Social.Queries.GetPersonaStats;
using AiPersona.Application.Features.Social.Queries.GetUserProfile;
using AiPersona.Application.Features.Social.Queries.GetFollowers;
using AiPersona.Application.Features.Social.Queries.GetFollowing;
using AiPersona.Application.Features.Social.Queries.GetBlockedUsers;
using AiPersona.Application.Features.Social.Queries.GetActivityFeed;

namespace AiPersona.Api.Controllers.V1;

public class SocialController : BaseApiController
{
    /// <summary>
    /// Toggle like on a persona
    /// </summary>
    [HttpPost("personas/{personaId:guid}/like")]
    [Authorize]
    public async Task<ActionResult> TogglePersonaLike(Guid personaId)
    {
        var result = await Mediator.Send(new ToggleLikeCommand(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Check if current user has liked a persona
    /// </summary>
    [HttpGet("personas/{personaId:guid}/liked")]
    [Authorize]
    public async Task<ActionResult> CheckPersonaLiked(Guid personaId)
    {
        var result = await Mediator.Send(new CheckLikedQuery(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Toggle favorite on a persona
    /// </summary>
    [HttpPost("personas/{personaId:guid}/favorite")]
    [Authorize]
    public async Task<ActionResult> TogglePersonaFavorite(Guid personaId)
    {
        var result = await Mediator.Send(new ToggleFavoriteCommand(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Check if current user has favorited a persona
    /// </summary>
    [HttpGet("personas/{personaId:guid}/favorited")]
    [Authorize]
    public async Task<ActionResult> CheckPersonaFavorited(Guid personaId)
    {
        var result = await Mediator.Send(new CheckFavoritedQuery(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get current user's favorited personas
    /// </summary>
    [HttpGet("favorites")]
    [Authorize]
    public async Task<ActionResult> GetUserFavorites(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var result = await Mediator.Send(new GetFavoritesQuery(limit, offset));
        return HandleResult(result);
    }

    /// <summary>
    /// Get persona social stats (likes, favorites, views)
    /// </summary>
    [HttpGet("personas/{personaId:guid}/stats")]
    [Authorize]
    public async Task<ActionResult> GetPersonaStats(Guid personaId)
    {
        var result = await Mediator.Send(new GetPersonaStatsQuery(personaId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get user public profile
    /// </summary>
    [HttpGet("users/{userId:guid}/profile")]
    [Authorize]
    public async Task<ActionResult> GetUserProfile(Guid userId)
    {
        var result = await Mediator.Send(new GetUserProfileQuery(userId));
        return HandleResult(result);
    }

    /// <summary>
    /// Toggle follow on a user
    /// </summary>
    [HttpPost("users/{userId:guid}/follow")]
    [Authorize]
    public async Task<ActionResult> ToggleFollow(Guid userId)
    {
        var result = await Mediator.Send(new ToggleFollowCommand(userId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get user's followers
    /// </summary>
    [HttpGet("users/{userId:guid}/followers")]
    [Authorize]
    public async Task<ActionResult> GetFollowers(
        Guid userId,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var result = await Mediator.Send(new GetFollowersQuery(userId, limit, offset));
        return HandleResult(result);
    }

    /// <summary>
    /// Get users that user is following
    /// </summary>
    [HttpGet("users/{userId:guid}/following")]
    [Authorize]
    public async Task<ActionResult> GetFollowing(
        Guid userId,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var result = await Mediator.Send(new GetFollowingQuery(userId, limit, offset));
        return HandleResult(result);
    }

    /// <summary>
    /// Block a user
    /// </summary>
    [HttpPost("users/{userId:guid}/block")]
    [Authorize]
    public async Task<ActionResult> BlockUser(Guid userId, [FromBody] BlockUserCommand command)
    {
        command = command with { UserId = userId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Unblock a user
    /// </summary>
    [HttpDelete("users/{userId:guid}/block")]
    [Authorize]
    public async Task<ActionResult> UnblockUser(Guid userId)
    {
        var result = await Mediator.Send(new UnblockUserCommand(userId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get blocked users
    /// </summary>
    [HttpGet("blocked")]
    [Authorize]
    public async Task<ActionResult> GetBlockedUsers(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var result = await Mediator.Send(new GetBlockedUsersQuery(limit, offset));
        return HandleResult(result);
    }

    /// <summary>
    /// Report content (persona, user, message)
    /// </summary>
    [HttpPost("report")]
    [Authorize]
    public async Task<ActionResult> ReportContent([FromBody] ReportContentCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get current user's activity feed
    /// </summary>
    [HttpGet("activity")]
    [Authorize]
    public async Task<ActionResult> GetActivityFeed(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var result = await Mediator.Send(new GetActivityFeedQuery(limit, offset));
        return HandleResult(result);
    }
}
