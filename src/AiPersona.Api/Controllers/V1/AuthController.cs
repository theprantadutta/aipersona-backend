using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Auth.Commands.Register;
using AiPersona.Application.Features.Auth.Commands.Login;
using AiPersona.Application.Features.Auth.Commands.FirebaseAuth;
using AiPersona.Application.Features.Auth.Commands.LinkGoogle;
using AiPersona.Application.Features.Auth.Commands.UnlinkGoogle;
using AiPersona.Application.Features.Auth.Commands.UpdateProfile;
using AiPersona.Application.Features.Auth.Queries.GetCurrentUser;
using AiPersona.Application.Features.Auth.Queries.GetAuthProviders;

namespace AiPersona.Api.Controllers.V1;

public class AuthController : BaseApiController
{
    /// <summary>
    /// Register a new user with email and password
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Authenticate with Firebase ID token
    /// </summary>
    [HttpPost("firebase")]
    [AllowAnonymous]
    public async Task<ActionResult> AuthenticateWithFirebase([FromBody] FirebaseAuthCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Authenticate with Google Sign-In (via Firebase)
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult> AuthenticateWithGoogle([FromBody] FirebaseAuthCommand command)
    {
        // Reuses Firebase authentication - Google Sign-In flows through Firebase
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Link Google account to existing email/password account
    /// </summary>
    [HttpPost("link-google")]
    [AllowAnonymous]
    public async Task<ActionResult> LinkGoogleAccount([FromBody] LinkGoogleCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Unlink Google account from user account
    /// </summary>
    [HttpPost("unlink-google")]
    [Authorize]
    public async Task<ActionResult> UnlinkGoogleAccount()
    {
        var result = await Mediator.Send(new UnlinkGoogleCommand());
        return HandleResult(result);
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> GetCurrentUser()
    {
        var result = await Mediator.Send(new GetCurrentUserQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get authentication providers linked to current user
    /// </summary>
    [HttpGet("providers")]
    [Authorize]
    public async Task<ActionResult> GetAuthProviders()
    {
        var result = await Mediator.Send(new GetAuthProvidersQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Logout (client should delete token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        // JWT tokens are stateless, so logout is handled client-side
        return Ok(new { message = "Logged out successfully" });
    }
}
