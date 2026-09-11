using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;

namespace TheOne.API.Controllers;

/// <summary>Exposes the signed-in user's profile and active refresh sessions.</summary>
[ApiController]
[Authorize]
[Route("api/v1/auth")]
[EnableRateLimiting("authentication")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
public sealed class UserSessionsController(IUserSessionService sessions) : ControllerBase
{
    /// <summary>Gets the current user's profile and current database role assignments.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> Me(CancellationToken cancellationToken) =>
        Ok(ApiResponse<CurrentUserResponse>.SuccessResponse(
            await sessions.GetCurrentUserAsync(UserId(), cancellationToken)));

    /// <summary>Lists active sessions; page size is limited to 100. Rotation preserves session identifiers.</summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<SessionListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SessionListResponse>>> List(
        CancellationToken cancellationToken, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20) =>
        Ok(ApiResponse<SessionListResponse>.SuccessResponse(await sessions.GetSessionsAsync(
            UserId(), Guid.TryParse(User.FindFirstValue("sid"), out var id) ? id : null,
            pageNumber, pageSize, cancellationToken)));

    /// <summary>Revokes one owned refresh session. Repeated or unknown identifiers return success without revealing ownership.</summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Revoke(Guid sessionId, CancellationToken cancellationToken)
    {
        await sessions.RevokeSessionAsync(UserId(), sessionId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Session revoked. Existing access tokens expire normally."));
    }

    /// <summary>Revokes all existing refresh sessions, including the caller's session.</summary>
    [HttpPost("logout-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> LogoutAll(CancellationToken cancellationToken)
    {
        await sessions.LogoutAllAsync(UserId(), cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "All refresh sessions revoked. Please sign in again."));
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue("sub"), out var id)
        ? id : throw new AuthenticationException("Authentication is required.", true);
}