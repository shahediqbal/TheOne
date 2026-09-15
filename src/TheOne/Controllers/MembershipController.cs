using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Common.Models;
using TheOne.Application.Membership;

namespace TheOne.API.Controllers;

/// <summary>
/// Public membership registration. Anonymous by design: no login exists for applicants yet.
/// No OTP and no payment step — those were explicitly ruled out for this flow.
/// </summary>
[ApiController]
[Route("api/v1/membership")]
[AllowAnonymous]
[EnableRateLimiting("membership-public")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
public sealed class MembershipController(IMembershipApplicationService applications) : ControllerBase
{
    [HttpGet("form-definition")]
    public IActionResult FormDefinition() => Ok(ApiResponse<object>.SuccessResponse(new {
        MembershipFormDefinition.Version, MembershipFormDefinition.RequiredFields,
        MembershipFormDefinition.ConductBn, MembershipFormDefinition.ConductEn,
        MembershipFormDefinition.DeclarationBn, MembershipFormDefinition.DeclarationEn,
        MembershipFormDefinition.OathBn, MembershipFormDefinition.OathEn
    }));

    /// <summary>Starts a new application and issues a reference code, returned with a private resume token; no SMS is sent.</summary>
    [HttpPost("applications")]
    [ProducesResponseType(typeof(ApiResponse<StartMembershipApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StartMembershipApplicationResponse>>> Start(
        StartMembershipApplicationRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<StartMembershipApplicationResponse>.SuccessResponse(
            await applications.StartAsync(request, cancellationToken),
            "Application started. Save your reference code and private resume token to continue later."));

    /// <summary>Resumes with reference code, private resume token and matching contact number.</summary>
    [HttpPost("applications/resume")]
    [ProducesResponseType(typeof(ApiResponse<MembershipApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MembershipApplicationResponse>>> Resume(
        ResumeMembershipApplicationRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MembershipApplicationResponse>.SuccessResponse(
            await applications.ResumeAsync(request, cancellationToken)));

    /// <summary>Saves any subset of section fields to an existing draft.</summary>
    [HttpPatch("applications/{referenceCode}")]
    [ProducesResponseType(typeof(ApiResponse<MembershipApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MembershipApplicationResponse>>> SaveSection(
        string referenceCode, SaveMembershipSectionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MembershipApplicationResponse>.SuccessResponse(
            await applications.SaveSectionAsync(referenceCode, request, cancellationToken),
            "Progress saved."));

    /// <summary>Finally submits a draft, moving it into the verification workflow.</summary>
    [HttpPost("applications/{referenceCode}/submit")]
    [ProducesResponseType(typeof(ApiResponse<MembershipApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MembershipApplicationResponse>>> Submit(
        string referenceCode, SubmitMembershipApplicationRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MembershipApplicationResponse>.SuccessResponse(
            await applications.SubmitAsync(referenceCode, request, cancellationToken),
            "Application submitted. Staff will verify it shortly."));
}
