using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheOne.Application.Authentication;
using TheOne.Application.UserManagement;
using TheOne.Persistence.Authentication;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.UserManagement;

/// <summary>Reads account pages and serializes administrative status changes.</summary>
public sealed class IdentityUserManagementStore(TheOneDbContext db, UserManager<ApplicationUser> users,
    ILogger<IdentityUserManagementStore> logger, TheOne.Application.Administration.IPermissionReader permissions) : IUserManagementStore
{
    /// <inheritdoc />
    public async Task<UserPageResponse> ListAsync(UserListRequest request, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToUpperInvariant();
            query = query.Where(x => (x.FullName != null && x.FullName.ToUpper().Contains(term)) ||
                (x.Email != null && x.Email.ToUpper().Contains(term)) || (x.PhoneNumber != null && x.PhoneNumber.Contains(search)));
        }
        var role = request.Role?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(role))
            query = query.Where(x => db.UserRoles.Any(ur => ur.UserId == x.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == role)));
        var count = await query.CountAsync(ct);
        var page = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new ManagedUserResponse(x.Id, x.FullName, x.Email, x.PhoneNumber, x.IsActive,
                x.EmailConfirmed, x.PhoneNumberConfirmed, x.TwoFactorEnabled, x.CreatedAtUtc, Array.Empty<string>())).ToArrayAsync(ct);
        var ids = page.Select(x => x.Id).ToArray();
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId) orderby r.Name select new { ur.UserId, r.Name }).ToArrayAsync(ct);
        return new UserPageResponse(page.Select(x => x with { Roles = roles.Where(r => r.UserId == x.Id).Select(r => r.Name!).ToArray() }).ToArray(),
            count, request.Page, request.PageSize);
    }

    /// <inheritdoc />
    public async Task<ManagedUserResponse?> GetAsync(Guid userId, CancellationToken ct)
    {
        var account = await db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new ManagedUserResponse(x.Id, x.FullName, x.Email, x.PhoneNumber, x.IsActive,
                x.EmailConfirmed, x.PhoneNumberConfirmed, x.TwoFactorEnabled, x.CreatedAtUtc, Array.Empty<string>())).SingleOrDefaultAsync(ct);
        if (account is null) return null;
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId orderby r.Name select r.Name!).ToArrayAsync(ct);
        return account with { Roles = roles };
    }

    /// <inheritdoc />
    public async Task<bool> ChangeStatusAsync(Guid actorId, Guid sessionId, Guid userId, bool isActive, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await TheOne.Persistence.Administration.AdministrationStore.LockConfigurationAsync(db, ct);
        foreach (var id in new[] { actorId, userId }.Distinct().OrderBy(x => x))
            await AuthenticationSecurity.LockAsync(db, id, ct);
        var actor = await users.FindByIdAsync(actorId.ToString());
        if (actor is null) throw AuthenticationSecurity.Invalid();
        // Authentication middleware can already have tracked this user before we acquired the lock.
        await db.Entry(actor).ReloadAsync(ct);
        var actorRoles = await users.GetRolesAsync(actor);
        if (!actor.IsActive || !actor.TwoFactorEnabled || await users.IsLockedOutAsync(actor) || !(await permissions.GetAsync(actorId, ct)).Contains(TheOne.Application.Administration.Permissions.UsersManage))
            throw AuthenticationSecurity.Invalid();
        var fingerprint = AuthenticationSecurity.Fingerprint(actorRoles);
        if (!await db.RefreshTokens.AnyAsync(x => x.UserId == actorId && x.SessionId == sessionId && x.MfaVerified &&
            x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow && x.SecurityStamp == actor.SecurityStamp && x.RolesFingerprint == fingerprint, ct))
            throw AuthenticationSecurity.Invalid();
        var target = await users.FindByIdAsync(userId.ToString());
        if (target is null) return false;
        await db.Entry(target).ReloadAsync(ct);
        var targetRoles = await users.GetRolesAsync(target);
        var superAdmin = actorRoles.Contains(LoginSecurityPolicy.SuperAdmin, StringComparer.OrdinalIgnoreCase);
        if (actorId == userId || targetRoles.Any(x => x.Equals(LoginSecurityPolicy.SuperAdmin, StringComparison.OrdinalIgnoreCase) || x.Equals("Super Admin", StringComparison.OrdinalIgnoreCase)) ||
            (!superAdmin && (LoginSecurityPolicy.IsAdministrator(targetRoles) || (await permissions.GetAsync(userId, ct)).Count > 0))) throw new UserManagementForbiddenException();
        if (target.IsActive == isActive) return true;
        target.IsActive = isActive;
        target.StatusChangedAtUtc = DateTime.UtcNow;
        AuthenticationSecurity.Ensure(await users.UpdateSecurityStampAsync(target));
        await AuthenticationSecurity.RevokeAsync(db, userId, "Account status changed.", ct);
        await db.MfaChallenges.Where(x => x.UserId == userId && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, target.StatusChangedAtUtc), ct);
        TheOne.Persistence.Administration.AdministrationStore.Audit(db, actorId, "User.StatusChanged", userId.ToString(), new { IsActive = isActive });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Account {UserId} active status changed to {IsActive} by {ActorId}", userId, isActive, actorId);
        return true;
    }
}
