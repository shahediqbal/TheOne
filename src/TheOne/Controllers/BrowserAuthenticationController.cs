using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;
namespace TheOne.API.Controllers;

/// <summary>Same-origin browser adapter; raw refresh credentials never enter JavaScript.</summary>
[ApiController, Route("api/v1/browser/auth"), BrowserRequest, EnableRateLimiting("authentication")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BrowserAuthenticationController(IAuthenticationService auth, IMfaService mfa, IAccountRecoveryService recovery) : ControllerBase
{
    private const string CookieName = "__Secure-TheOneRefresh";
    private static CookieOptions Options(DateTime? expiry = null) => new() { HttpOnly=true, Secure=true, SameSite=SameSiteMode.Strict, Path="/api/v1/browser/auth", Expires=expiry };
    private void SetCookie(TokenResponse token) => Response.Cookies.Append(CookieName, token.RefreshToken, Options(token.RefreshTokenExpiresAtUtc));
    private ActionResult<ApiResponse<BrowserLoginResponse>> Complete(TokenResponse token)
    { SetCookie(token); return Ok(ApiResponse<BrowserLoginResponse>.SuccessResponse(new(token.AccessToken,token.ExpiresAtUtc,false,false,null),"Signed in.")); }
    /// <summary>Password sign-in, returning either access credentials or an MFA challenge.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<BrowserLoginResponse>>> Login(LoginRequest request,CancellationToken ct)
    {
        var result=await auth.LoginAsync(request,ct);
        if(result.AccessToken is not null && result.RefreshToken is not null)
            SetCookie(new TokenResponse(result.AccessToken,result.RefreshToken,result.ExpiresAtUtc!.Value,result.RefreshTokenExpiresAtUtc!.Value));
        return Ok(ApiResponse<BrowserLoginResponse>.SuccessResponse(new(result.AccessToken,result.ExpiresAtUtc,result.RequiresTwoFactor,result.RequiresAuthenticatorSetup,result.ChallengeId),"Login step completed."));
    }
    /// <summary>Completes authenticator sign-in.</summary>
    [HttpPost("authenticator")]
    public async Task<ActionResult<ApiResponse<BrowserLoginResponse>>> Authenticator(MfaCodeRequest request,CancellationToken ct) => Complete(await mfa.CompleteLoginAsync(request,ct));
    /// <summary>Completes recovery-code sign-in.</summary>
    [HttpPost("recovery-code")]
    public async Task<ActionResult<ApiResponse<BrowserLoginResponse>>> Recovery(MfaRecoveryRequest request,CancellationToken ct) => Complete(await mfa.CompleteRecoveryLoginAsync(request,ct));
    /// <summary>Completes ordinary SMS sign-in.</summary>
    [HttpPost("otp")]
    public async Task<ActionResult<ApiResponse<BrowserLoginResponse>>> Otp(VerifyLoginOtpRequest request,CancellationToken ct) => Complete(await recovery.LoginWithOtpAsync(request,ct));
    /// <summary>Rotates the browser refresh credential.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<BrowserLoginResponse>>> Refresh(CancellationToken ct)
    {
        if(!Request.Cookies.TryGetValue(CookieName,out var raw)) return Unauthorized(ApiResponse<object>.FailureResponse("Please sign in."));
        try { return Complete(await auth.RefreshAsync(new RefreshTokenRequest { RefreshToken=raw },ct)); }
        catch(AuthenticationException) { Response.Cookies.Delete(CookieName,Options()); throw; }
    }
    /// <summary>Revokes the cookie session and clears the browser credential.</summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(CancellationToken ct)
    {
        if(Request.Cookies.TryGetValue(CookieName,out var raw)) await auth.LogoutAsync(new RefreshTokenRequest {RefreshToken=raw},ct);
        Response.Cookies.Delete(CookieName,Options());
        return Ok(ApiResponse<bool>.SuccessResponse(true,"Signed out."));
    }
}
/// <summary>JavaScript-visible authentication response excludes refresh credentials.</summary>
public sealed record BrowserLoginResponse(string? AccessToken,DateTime? ExpiresAtUtc,bool RequiresTwoFactor,bool RequiresAuthenticatorSetup,Guid? ChallengeId);
/// <summary>Rejects cross-origin cookie actions and non-HTTPS browser authentication.</summary>
public sealed class BrowserRequestAttribute : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var req=context.HttpContext.Request;
        var origin=req.Headers.Origin.ToString();
        var config=context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var environment=context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var allowed=config.GetSection("Browser:AllowedOrigins").Get<string[]>() ?? [];
        var trusted=origin==$"{req.Scheme}://{req.Host}" || allowed.Contains(origin,StringComparer.Ordinal) ||
            (environment.IsDevelopment() && origin=="https://localhost:5173");
        if(!req.IsHttps || req.Headers["X-TheOne-Client"]!="web" || string.IsNullOrEmpty(origin) || !trusted)
            context.Result=new ObjectResult(ApiResponse<object>.FailureResponse("Browser request origin is not allowed.")){StatusCode=403};
    }
}
