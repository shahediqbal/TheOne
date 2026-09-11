using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;
namespace TheOne.API.Controllers;

/// <summary>Provides administrator promotion only through a currently verified SuperAdmin session.</summary>
[ApiController]
[Authorize(Roles = LoginSecurityPolicy.SuperAdmin)]
[Route("api/v1/admin/users")]
[EnableRateLimiting("authentication")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdministratorRolesController(IAdministratorRoleService roles) : ControllerBase
{
    /// <summary>Grants Admin or SuperAdmin to an already-enrolled user and revokes that user's sessions.</summary>
    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> Assign(Guid userId, AssignAdministratorRoleRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var actor) || !Guid.TryParse(User.FindFirstValue("sid"), out var session))
            return Unauthorized(ApiResponse<object>.FailureResponse("Authentication is required."));
        await roles.AssignAsync(actor, session, userId, request, ct);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Role assigned. The user must sign in again."));
    }
}