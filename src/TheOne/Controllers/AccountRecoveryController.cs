using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;

namespace TheOne.API.Controllers;

/// <summary>Provides mobile verification and password recovery with single-use SMS codes.</summary>
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("authentication")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
public sealed class AccountRecoveryController(IAccountRecoveryService recovery) : ControllerBase
{
    /// <summary>Requests a code for the signed-in user's registered mobile; no request body is required.</summary>
    [Authorize]
    [HttpPost("request-mobile-verification")]
    [ProducesResponseType(typeof(ApiResponse<OtpChallengeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<OtpChallengeResponse>>> RequestMobileVerification(CancellationToken cancellationToken) =>
        Ok(ApiResponse<OtpChallengeResponse>.SuccessResponse(
            await recovery.RequestMobileVerificationAsync(UserId(), cancellationToken), "Verification code submitted."));

    /// <summary>Confirms the signed-in user's mobile using the challenge identifier and six-digit code.</summary>
    [Authorize]
    [HttpPost("verify-mobile")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyMobile(VerifyMobileRequest request, CancellationToken cancellationToken)
    {
        await recovery.VerifyMobileAsync(UserId(), request, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Mobile number verified."));
    }

    /// <summary>Requests a password-reset code for a previously verified mobile. Always returns a generic acknowledgement.</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<OtpChallengeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<OtpChallengeResponse>>> ForgotPassword(
        ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<OtpChallengeResponse>.SuccessResponse(
            await recovery.ForgotPasswordAsync(request, cancellationToken),
            "If the account is eligible, a recovery code will be sent to its verified mobile."));

    /// <summary>Consumes a recovery code, resets the password, and revokes all refresh sessions.</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await recovery.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Password reset. Please sign in again."));
    }

    /// <summary>Requests a login-only SMS code for an eligible regular user's verified mobile.</summary>
    [AllowAnonymous]
    [HttpPost("request-login-otp")]
    [ProducesResponseType(typeof(ApiResponse<OtpChallengeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OtpChallengeResponse>>> RequestLoginOtp(SmsLoginRequest request, CancellationToken ct) =>
        Ok(ApiResponse<OtpChallengeResponse>.SuccessResponse(await recovery.RequestLoginOtpAsync(request, ct),
            "If the account is eligible, a login code will be sent to its verified mobile."));

    /// <summary>Consumes a login OTP and creates a regular-user session. Administrators cannot use this path.</summary>
    [AllowAnonymous]
    [HttpPost("login-with-otp")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> LoginWithOtp(VerifyLoginOtpRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TokenResponse>.SuccessResponse(await recovery.LoginWithOtpAsync(request, ct), "Login successful."));

    private Guid UserId() => Guid.TryParse(User.FindFirstValue("sub"), out var id)
        ? id : throw new AuthenticationException("Authentication is required.", true);
}
