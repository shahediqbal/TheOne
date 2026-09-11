using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;

namespace TheOne.API.Controllers;

/// <summary>Provides password authentication and rotating refresh-session endpoints.</summary>
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("authentication")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
public sealed class AuthController(IAuthenticationService authentication) : ControllerBase
{
    /// <summary>Creates a Member account. Sign in separately to create a session.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(
        RegisterRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RegisterResponse>.SuccessResponse(
            await authentication.RegisterAsync(request, cancellationToken), "Registration successful."));

    /// <summary>Checks a password; returns tokens only when no second factor is required.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        LoginRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<LoginResponse>.SuccessResponse(
            await authentication.LoginAsync(request, cancellationToken), "Login step completed."));

    /// <summary>Consumes a refresh token once and returns a replacement token pair.</summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh(
        RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TokenResponse>.SuccessResponse(
            await authentication.RefreshAsync(request, cancellationToken), "Session refreshed."));

    /// <summary>Revokes the supplied refresh session, including its rotated replacements.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(
        RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authentication.LogoutAsync(request, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Logged out."));
    }

    /// <summary>Changes the current user's password and revokes all refresh sessions.</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var userId))
            return Unauthorized(ApiResponse<object>.FailureResponse("Authentication is required."));
        await authentication.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Password changed. Please sign in again."));
    }
}