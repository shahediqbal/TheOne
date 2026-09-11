using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;
namespace TheOne.Persistence.Authentication;

/// <summary>Serializes administrator promotion and requires an active SuperAdmin MFA session.</summary>
public sealed class AdministratorRoleStore(TheOneDbContext db, UserManager<ApplicationUser> users,
    ILogger<AdministratorRoleStore> logger) : IAdministratorRoleStore
{
    /// <inheritdoc />
    public async Task AssignAsync(Guid actorId, Guid actorSessionId, Guid userId, AssignAdministratorRoleRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await TheOne.Persistence.Administration.AdministrationStore.LockConfigurationAsync(db, ct);
        foreach (var id in new[] { actorId, userId }.Distinct().OrderBy(x => x))
            await AuthenticationSecurity.LockAsync(db, id, ct);
        var actor = await users.FindByIdAsync(actorId.ToString());
        if (actor is not null) await db.Entry(actor).ReloadAsync(ct);
        if (actor is null || !actor.IsActive || !actor.TwoFactorEnabled || await users.IsLockedOutAsync(actor) ||
            !await users.IsInRoleAsync(actor, LoginSecurityPolicy.SuperAdmin)) throw AuthenticationSecurity.Invalid();
        var actorRoles = AuthenticationSecurity.Fingerprint(await users.GetRolesAsync(actor));
        if (!await db.RefreshTokens.AnyAsync(x => x.UserId == actorId && x.SessionId == actorSessionId &&
            x.MfaVerified && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow &&
            x.SecurityStamp == actor.SecurityStamp && x.RolesFingerprint == actorRoles, ct)) throw AuthenticationSecurity.Invalid();
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is not null) await db.Entry(user).ReloadAsync(ct);
        if (user is null || !user.IsActive || !user.TwoFactorEnabled || await users.IsLockedOutAsync(user) ||
            (!user.PhoneNumberConfirmed && !user.EmailConfirmed) || string.IsNullOrEmpty(await users.GetAuthenticatorKeyAsync(user)))
            throw new AuthenticationException("The target must be active, contact-verified, and authenticator-enrolled.");
        if (await users.IsInRoleAsync(user, request.Role)) return;
        AuthenticationSecurity.Ensure(await users.AddToRoleAsync(user, request.Role));
        AuthenticationSecurity.Ensure(await users.UpdateSecurityStampAsync(user));
        await AuthenticationSecurity.RevokeAsync(db, userId, "Administrator role assigned.", ct);
        TheOne.Persistence.Administration.AdministrationStore.Audit(db, actorId, "User.RoleAssigned", userId.ToString(), new { Role = request.Role });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Role {Role} assigned to {UserId} by {ActorId}", request.Role, userId, actorId);
    }
}