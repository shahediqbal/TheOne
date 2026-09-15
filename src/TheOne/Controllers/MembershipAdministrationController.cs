using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheOne.Application.Administration;
using TheOne.Application.Common.Models;
using TheOne.Application.Membership;

namespace TheOne.API.Controllers;

/// <summary>Staff review workflow: verify, approve (fee-gated), reject, and record contributions.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/membership")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
public sealed class MembershipAdministrationController(IMembershipAdministrationService service) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue("sub")!);

    /// <summary>Marks a submitted application as staff-verified.</summary>
    [HttpPost("applications/{referenceCode}/verify")]
    [Authorize(Policy = Permissions.MembershipReview)]
    public async Task<ActionResult<ApiResponse<MemberResponse>>> Verify(string referenceCode, CancellationToken ct) =>
        Ok(ApiResponse<MemberResponse>.SuccessResponse(await service.VerifyAsync(referenceCode, Actor, ct), "Application verified."));

    /// <summary>Approves a verified application. Fails with 409 if the 100 BDT membership fee has not been recorded.</summary>
    [HttpPost("applications/{referenceCode}/approve")]
    [Authorize(Policy = Permissions.MembershipApprove)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MemberResponse>>> Approve(string referenceCode, CancellationToken ct) =>
        Ok(ApiResponse<MemberResponse>.SuccessResponse(await service.ApproveAsync(referenceCode, Actor, ct), "Application approved."));

    /// <summary>Rejects an application with a reason.</summary>
    [HttpPost("applications/{referenceCode}/reject")]
    [Authorize(Policy = Permissions.MembershipApprove)]
    public async Task<ActionResult<ApiResponse<MemberResponse>>> Reject(
        string referenceCode, RejectMembershipApplicationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<MemberResponse>.SuccessResponse(await service.RejectAsync(referenceCode, Actor, request, ct), "Application rejected."));

    /// <summary>Records a payment (membership fee, monthly due, event contribution, or donation) against a member.</summary>
    [HttpPost("applications/{referenceCode}/contributions")]
    [Authorize(Policy = Permissions.MembershipContribute)]
    public async Task<ActionResult<ApiResponse<ContributionResponse>>> RecordContribution(
        string referenceCode, RecordContributionRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContributionResponse>.SuccessResponse(await service.RecordContributionAsync(referenceCode, Actor, request, ct), "Contribution recorded."));
}
