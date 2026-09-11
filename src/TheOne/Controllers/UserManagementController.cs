using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheOne.Application.Common.Models;
using TheOne.Application.UserManagement;

namespace TheOne.API.Controllers;

/// <summary>Administrative account listing, details and status management.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/users")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public sealed class UserManagementController(IUserManagementService service) : ControllerBase
{
    /// <summary>Lists users with search, role/status filters and bounded pagination.</summary>
    [HttpGet, Authorize(Policy = TheOne.Application.Administration.Permissions.UsersRead)]
    [ProducesResponseType(typeof(ApiResponse<UserPageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserPageResponse>>> List([FromQuery] UserListRequest request, CancellationToken ct) =>
        Ok(ApiResponse<UserPageResponse>.SuccessResponse(await service.ListAsync(request, ct), "Users retrieved."));

    /// <summary>Returns safe account details and assigned role names.</summary>
    [HttpGet("{userId:guid}"), Authorize(Policy = TheOne.Application.Administration.Permissions.UsersRead)]
    [ProducesResponseType(typeof(ApiResponse<ManagedUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ManagedUserResponse>>> Get(Guid userId, CancellationToken ct)
    {
        var user = await service.GetAsync(userId, ct);
        return user is null ? NotFound(ApiResponse<object>.FailureResponse("User not found."))
            : Ok(ApiResponse<ManagedUserResponse>.SuccessResponse(user, "User retrieved."));
    }

    /// <summary>Activates/deactivates an eligible account and invalidates previous login sessions.</summary>
    [HttpPatch("{userId:guid}/status"), Authorize(Policy = TheOne.Application.Administration.Permissions.UsersManage)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangeStatus(Guid userId, ChangeUserStatusRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var actor) || !Guid.TryParse(User.FindFirstValue("sid"), out var session))
            return Unauthorized(ApiResponse<object>.FailureResponse("Authentication is required."));
        try
        {
            return await service.ChangeStatusAsync(actor, session, userId, request, ct)
                ? Ok(ApiResponse<bool>.SuccessResponse(true, "Account status updated."))
                : NotFound(ApiResponse<object>.FailureResponse("User not found."));
        }
        catch (UserManagementForbiddenException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResponse("You cannot change this account's status."));
        }
    }
}
