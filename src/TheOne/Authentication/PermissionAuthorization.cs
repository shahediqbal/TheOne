using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TheOne.Application.Administration;
namespace TheOne.API.Authentication;

/// <summary>Requires a current database grant and MFA for administrative operations.</summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
/// <summary>Evaluates permissions independently of navigation visibility.</summary>
public sealed class PermissionAuthorizationHandler(IPermissionReader reader) : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!context.User.HasClaim("amr", "mfa") || !Guid.TryParse(context.User.FindFirstValue("sub"), out var userId)) return;
        var ct = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        if ((await reader.GetAsync(userId, ct)).Contains(requirement.Permission)) context.Succeed(requirement);
    }
}
