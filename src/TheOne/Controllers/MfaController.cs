using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;

namespace TheOne.API.Controllers;

/// <summary>Provides authenticator enrollment, password-proven MFA login, and protected recovery management.</summary>
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("authentication")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
public sealed class MfaController(IMfaService mfa) : ControllerBase
{
    /// <summary>Starts enrollment for a signed-in, contact-verified regular user after password confirmation.</summary>
    [Authorize]
    [HttpPost("authenticator/begin-enrollment")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Begin(PasswordConfirmationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<LoginResponse>.SuccessResponse(await mfa.BeginEnrollmentAsync(UserId(), request, ct)));

    /// <summary>Returns the shared key and PNG QR data URI for a password-proven enrollment challenge.</summary>
    [AllowAnonymous]
    [HttpPost("authenticator/setup")]
    [ProducesResponseType(typeof(ApiResponse<AuthenticatorSetupResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthenticatorSetupResponse>>> Setup(MfaChallengeRequest request, CancellationToken ct) =>
        Ok(ApiResponse<AuthenticatorSetupResponse>.SuccessResponse(await mfa.GetSetupAsync(request, ct)));

    /// <summary>Confirms enrollment and shows ten recovery codes once. Sign in again afterward.</summary>
    [AllowAnonymous]
    [HttpPost("authenticator/confirm-setup")]
    [ProducesResponseType(typeof(ApiResponse<RecoveryCodesResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecoveryCodesResponse>>> Confirm(MfaCodeRequest request, CancellationToken ct) =>
        Ok(ApiResponse<RecoveryCodesResponse>.SuccessResponse(await mfa.ConfirmSetupAsync(request, ct), "Authenticator enabled. Save your recovery codes and sign in again."));

    /// <summary>Exchanges a password-proven login challenge and fresh authenticator code for session tokens.</summary>
    [AllowAnonymous]
    [HttpPost("login-with-authenticator")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login(MfaCodeRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TokenResponse>.SuccessResponse(await mfa.CompleteLoginAsync(request, ct), "Login successful."));

    /// <summary>Exchanges a password-proven login challenge and single-use recovery code for session tokens.</summary>
    [AllowAnonymous]
    [HttpPost("login-with-recovery-code")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> RecoveryLogin(MfaRecoveryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TokenResponse>.SuccessResponse(await mfa.CompleteRecoveryLoginAsync(request, ct), "Login successful."));

    /// <summary>Requires password plus current second-factor proof, revokes sessions, and starts mandatory re-enrollment.</summary>
    [Authorize]
    [HttpPost("authenticator/reset")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Reset(AuthenticatorResetRequest request, CancellationToken ct) =>
        Ok(ApiResponse<LoginResponse>.SuccessResponse(await mfa.ResetAsync(UserId(), request, ct), "Re-enroll your authenticator before signing in."));

    /// <summary>Replaces recovery codes after fresh password and second-factor proof; revokes existing sessions.</summary>
    [Authorize]
    [HttpPost("authenticator/regenerate-recovery-codes")]
    [ProducesResponseType(typeof(ApiResponse<RecoveryCodesResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecoveryCodesResponse>>> Regenerate(AuthenticatorResetRequest request, CancellationToken ct) =>
        Ok(ApiResponse<RecoveryCodesResponse>.SuccessResponse(await mfa.RegenerateRecoveryCodesAsync(UserId(), request, ct)));

    private Guid UserId() => Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : throw AuthenticationSecurityError();
    private static AuthenticationException AuthenticationSecurityError() => new("Authentication is required.", true);
}