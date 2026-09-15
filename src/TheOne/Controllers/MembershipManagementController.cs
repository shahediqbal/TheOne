using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheOne.Application.Administration;
using TheOne.Application.Common.Models;
using TheOne.Application.Membership;
namespace TheOne.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/membership")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MembershipManagementController(IMembershipManagement management) : ControllerBase
{
    [HttpGet("applications")]
    [Authorize(Policy = Permissions.MembershipRead)]
    public async Task<ActionResult<ApiResponse<MembershipPage>>> List([FromQuery] MembershipQuery query, CancellationToken ct) =>
        Ok(ApiResponse<MembershipPage>.SuccessResponse(await management.ListAsync(query, ct)));
    [HttpGet("applications/{referenceCode}")]
    [Authorize(Policy = Permissions.MembershipRead)]
    public async Task<ActionResult<ApiResponse<MembershipDetail>>> Detail(string referenceCode, CancellationToken ct) =>
        Ok(ApiResponse<MembershipDetail>.SuccessResponse(await management.DetailAsync(referenceCode, ct)));
    [HttpPost("operator")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [Authorize(Policy = Permissions.MembershipRead)]
    [Authorize(Policy = Permissions.MembershipEnter)]
    public async Task<ActionResult<ApiResponse<MembershipDetail>>> Enter(OperatorMembershipRequest request, CancellationToken ct) =>
        Ok(ApiResponse<MembershipDetail>.SuccessResponse(await management.EnterAsync(Guid.Parse(User.FindFirstValue("sub")!), request, ct)));
}
