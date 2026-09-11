using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheOne.Application.Administration;
using TheOne.Application.Authentication;
using TheOne.Application.Common.Models;
namespace TheOne.API.Controllers;

/// <summary>SuperAdmin role and menu configuration, plus authorized audit queries.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Produces("application/json")]
public sealed class AdministrationController(IAdministrationService service) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue("sub")!);
    private Guid Session => Guid.TryParse(User.FindFirstValue("sid"), out var id) ? id : throw new AuthenticationException("Authentication is required.", true);
    /// <summary>Lists the server-defined permission catalog.</summary>
    [HttpGet("permissions"), Authorize(Roles = "SuperAdmin")]
    public ActionResult<ApiResponse<IReadOnlyCollection<string>>> PermissionsList() => Ok(ApiResponse<IReadOnlyCollection<string>>.SuccessResponse(Permissions.All, "Permissions retrieved."));
    /// <summary>Lists roles and their current grants.</summary>
    [HttpGet("roles"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleResponse>>>> Roles(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyCollection<RoleResponse>>.SuccessResponse(await service.RolesAsync(ct), "Roles retrieved."));
    /// <summary>Creates a custom role.</summary>
    [HttpPost("roles"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole(RoleRequest request, CancellationToken ct) =>
        Ok(ApiResponse<RoleResponse>.SuccessResponse(await service.SaveRoleAsync(Actor, Session, null, request, ct), "Role created."));
    /// <summary>Updates a role name and replaces its permission set.</summary>
    [HttpPut("roles/{id:guid}"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole(Guid id, RoleRequest request, CancellationToken ct) =>
        Ok(ApiResponse<RoleResponse>.SuccessResponse(await service.SaveRoleAsync(Actor, Session, id, request, ct), "Role updated."));
    /// <summary>Deletes an unused custom role.</summary>
    [HttpDelete("roles/{id:guid}"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(Guid id, CancellationToken ct)
    { await service.DeleteRoleAsync(Actor, Session, id, ct); return Ok(ApiResponse<bool>.SuccessResponse(true, "Role deleted.")); }
    /// <summary>Assigns a role; administrative grants require verified authenticator enrollment.</summary>
    [HttpPut("users/{userId:guid}/roles/{roleId:guid}"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Assign(Guid userId, Guid roleId, CancellationToken ct)
    { await service.AssignRoleAsync(Actor, Session, userId, roleId, true, ct); return Ok(ApiResponse<bool>.SuccessResponse(true, "Role assigned. Sign in again.")); }
    /// <summary>Removes an eligible role assignment and revokes target sessions.</summary>
    [HttpDelete("users/{userId:guid}/roles/{roleId:guid}"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Remove(Guid userId, Guid roleId, CancellationToken ct)
    { await service.AssignRoleAsync(Actor, Session, userId, roleId, false, ct); return Ok(ApiResponse<bool>.SuccessResponse(true, "Role removed.")); }
    /// <summary>Lists menu definitions and role visibility assignments.</summary>
    [HttpGet("menus"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MenuResponse>>>> Menus(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyCollection<MenuResponse>>.SuccessResponse(await service.MenusAsync(ct), "Menus retrieved."));
    /// <summary>Creates a menu with role assignments.</summary>
    [HttpPost("menus"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<MenuResponse>>> CreateMenu(MenuRequest request, CancellationToken ct) =>
        Ok(ApiResponse<MenuResponse>.SuccessResponse(await service.SaveMenuAsync(Actor, Session, null, request, ct), "Menu created."));
    /// <summary>Updates menu content, location, status and complete role assignments.</summary>
    [HttpPut("menus/{id:guid}"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<MenuResponse>>> UpdateMenu(Guid id, MenuRequest request, CancellationToken ct) =>
        Ok(ApiResponse<MenuResponse>.SuccessResponse(await service.SaveMenuAsync(Actor, Session, id, request, ct), "Menu updated."));
    /// <summary>Deletes a menu without children.</summary>
    [HttpDelete("menus/{id:guid}"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteMenu(Guid id, CancellationToken ct)
    { await service.DeleteMenuAsync(Actor, Session, id, ct); return Ok(ApiResponse<bool>.SuccessResponse(true, "Menu deleted.")); }
    /// <summary>Reads security events by actor, exact action and UTC date range.</summary>
    [HttpGet("audit"), Authorize(Policy = Permissions.AuditRead)]
    public async Task<ActionResult<ApiResponse<AuditPage>>> Audit([FromQuery] AuditQuery request, CancellationToken ct) =>
        Ok(ApiResponse<AuditPage>.SuccessResponse(await service.AuditAsync(request, ct), "Audit history retrieved."));
}

/// <summary>Returns current navigation and effective administrative permissions.</summary>
[ApiController, Authorize, Route("api/v1/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class NavigationController(IAdministrationService service, IPermissionReader reader) : ControllerBase
{
    /// <summary>Returns enabled, role-assigned menus whose ancestors and permission checks also allow access.</summary>
    [HttpGet("menus")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NavigationNode>>>> Menus(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyCollection<NavigationNode>>.SuccessResponse(await service.NavigationAsync(Guid.Parse(User.FindFirstValue("sub")!), User.HasClaim("amr", "mfa"), ct), "Navigation retrieved."));
    /// <summary>Returns usable permissions for the current session.</summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<string>>>> PermissionsList(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyCollection<string>>.SuccessResponse(User.HasClaim("amr", "mfa") ?
            await reader.GetAsync(Guid.Parse(User.FindFirstValue("sub")!), ct) : Array.Empty<string>(), "Permissions retrieved."));
}
