using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheOne.Application.Administration;
using TheOne.Application.Authentication;
using TheOne.Domain.Entities;
using TheOne.Persistence.Authentication;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;
namespace TheOne.Persistence.Administration;

/// <summary>Transactional role, menu and security-history operations.</summary>
public sealed class AdministrationStore(TheOneDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roles,
    IPermissionReader permissions) : IAdministrationStore
{
    /// <summary>Shared lock for role/menu administration and legacy administrator promotion.</summary>
    public static Task<int> LockConfigurationAsync(TheOneDbContext db, CancellationToken ct) =>
        db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(914100301)", ct);
    /// <summary>Adds safe audit metadata within the caller's transaction.</summary>
    public static void Audit(TheOneDbContext db, Guid? actor, string action, string target, object details) =>
        db.SecurityAuditEvents.Add(new SecurityAuditEvent { ActorId = actor, Action = action, Target = target, Details = JsonSerializer.Serialize(details) });

    private async Task AuthorizeAsync(Guid actor, Guid session, CancellationToken ct)
    {
        await AuthenticationSecurity.LockAsync(db, actor, ct);
        var user = await users.FindByIdAsync(actor.ToString()) ?? throw AuthenticationSecurity.Invalid();
        await db.Entry(user).ReloadAsync(ct);
        if (!user.IsActive || !user.TwoFactorEnabled || await users.IsLockedOutAsync(user) || !await users.IsInRoleAsync(user, "SuperAdmin"))
            throw new AdministrationException("SuperAdmin access is required.", 403);
        var fingerprint = AuthenticationSecurity.Fingerprint(await users.GetRolesAsync(user));
        if (!await db.RefreshTokens.AnyAsync(x => x.UserId == actor && x.SessionId == session && x.MfaVerified &&
            x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow && x.SecurityStamp == user.SecurityStamp && x.RolesFingerprint == fingerprint, ct))
            throw AuthenticationSecurity.Invalid();
    }
    private static bool SystemRole(string? name) => name is not null && new[] { "MEMBER", "ADMIN", "SUPERADMIN", "SUPER ADMIN" }.Contains(name.ToUpperInvariant());

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RoleResponse>> RolesAsync(CancellationToken ct)
    {
        var list = await db.Roles.AsNoTracking().OrderBy(x => x.Name).ToArrayAsync(ct);
        var grants = await db.RoleClaims.AsNoTracking().Where(x => x.ClaimType == Permissions.ClaimType).ToArrayAsync(ct);
        return list.Select(r => new RoleResponse(r.Id, r.Name!, SystemRole(r.Name), r.NormalizedName == "SUPERADMIN" ? Permissions.All :
            grants.Where(g => g.RoleId == r.Id).Select(g => g.ClaimValue!).Distinct().Order().ToArray())).ToArray();
    }

    /// <inheritdoc />
    public async Task<RoleResponse> SaveRoleAsync(Guid actor, Guid session, Guid? id, RoleRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockConfigurationAsync(db, ct); await AuthorizeAsync(actor, session, ct);
        var role = id.HasValue ? await roles.FindByIdAsync(id.Value.ToString()) ?? throw new AdministrationException("Role not found.", 404)
            : new IdentityRole<Guid> { Id = Guid.NewGuid() };
        var name = request.Name.Trim();
        if (id is null && SystemRole(name)) throw new AdministrationException("System role names are reserved.");
        if (id.HasValue && SystemRole(role.Name) && (role.NormalizedName != "ADMIN" || name != role.Name))
            throw new AdministrationException("This system role is protected.");
        if (id.HasValue && !SystemRole(role.Name) && SystemRole(name)) throw new AdministrationException("System role names are reserved.");
        if (id.HasValue && role.Name != name && await db.UserRoles.AnyAsync(x => x.RoleId == role.Id, ct))
            throw new AdministrationException("An assigned role cannot be renamed.", 409);
        var beforeName = role.Name;
        var before = await db.RoleClaims.Where(x => x.RoleId == role.Id && x.ClaimType == Permissions.ClaimType).Select(x => x.ClaimValue!).ToArrayAsync(ct);
        role.Name = name;
        AuthenticationSecurity.Ensure(id is null ? await roles.CreateAsync(role) : await roles.UpdateAsync(role));
        await db.RoleClaims.Where(x => x.RoleId == role.Id && x.ClaimType == Permissions.ClaimType).ExecuteDeleteAsync(ct);
        foreach (var grant in request.Permissions)
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = Permissions.ClaimType, ClaimValue = grant });
        Audit(db, actor, id is null ? "Role.Created" : "Role.Updated", role.Id.ToString(), new { BeforeName = beforeName, Name = name, BeforePermissions = before, Permissions = request.Permissions });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new RoleResponse(role.Id, name, SystemRole(name), request.Permissions);
    }
    /// <inheritdoc />
    public async Task DeleteRoleAsync(Guid actor, Guid session, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockConfigurationAsync(db, ct); await AuthorizeAsync(actor, session, ct);
        var role = await roles.FindByIdAsync(id.ToString()) ?? throw new AdministrationException("Role not found.", 404);
        if (SystemRole(role.Name)) throw new AdministrationException("System roles cannot be deleted.");
        if (await db.UserRoles.AnyAsync(x => x.RoleId == id, ct) || await db.NavigationMenuRoles.AnyAsync(x => x.RoleId == id, ct))
            throw new AdministrationException("Remove user and menu assignments before deleting this role.", 409);
        AuthenticationSecurity.Ensure(await roles.DeleteAsync(role));
        Audit(db, actor, "Role.Deleted", id.ToString(), new { role.Name });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <inheritdoc />
    public async Task AssignRoleAsync(Guid actor, Guid session, Guid userId, Guid roleId, bool assign, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockConfigurationAsync(db, ct);
        foreach (var id in new[] { actor, userId }.Distinct().OrderBy(x => x)) await AuthenticationSecurity.LockAsync(db, id, ct);
        await AuthorizeAsync(actor, session, ct);
        if (actor == userId) throw new AdministrationException("You cannot change your own role assignments.", 403);
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw new AdministrationException("User not found.", 404);
        await db.Entry(user).ReloadAsync(ct);
        var role = await roles.FindByIdAsync(roleId.ToString()) ?? throw new AdministrationException("Role not found.", 404);
        if (!assign && (role.NormalizedName is "SUPERADMIN" or "SUPER ADMIN" or "MEMBER"))
            throw new AdministrationException("This system-role assignment is protected.", 403);
        var hasRole = await users.IsInRoleAsync(user, role.Name!);
        if (hasRole == assign) return;
        var administrative = LoginSecurityPolicy.IsAdministrator(new[] { role.Name! }) ||
            await db.RoleClaims.AnyAsync(x => x.RoleId == roleId && x.ClaimType == Permissions.ClaimType, ct);
        if (assign && (!user.IsActive || await users.IsLockedOutAsync(user) || (administrative &&
            (!user.TwoFactorEnabled || (!user.EmailConfirmed && !user.PhoneNumberConfirmed) || string.IsNullOrEmpty(await users.GetAuthenticatorKeyAsync(user))))))
            throw new AdministrationException("Administrative role assignment requires an active, verified, authenticator-enrolled account.");
        AuthenticationSecurity.Ensure(assign ? await users.AddToRoleAsync(user, role.Name!) : await users.RemoveFromRoleAsync(user, role.Name!));
        AuthenticationSecurity.Ensure(await users.UpdateSecurityStampAsync(user));
        await AuthenticationSecurity.RevokeAsync(db, userId, "Role assignment changed.", ct);
        Audit(db, actor, assign ? "User.RoleAssigned" : "User.RoleRemoved", userId.ToString(), new { RoleId = roleId, Role = role.Name });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<MenuResponse>> MenusAsync(CancellationToken ct)
    {
        var menus = await db.NavigationMenus.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToArrayAsync(ct);
        var links = await db.NavigationMenuRoles.AsNoTracking().ToArrayAsync(ct);
        return menus.Select(x => new MenuResponse(x.Id, x.LabelEn, x.LabelBn, x.Icon, x.Route, x.ParentId, x.SortOrder, x.Enabled,
            x.RequiredPermission, links.Where(l => l.MenuId == x.Id).Select(l => l.RoleId).ToArray())).ToArray();
    }
    /// <inheritdoc />
    public async Task<MenuResponse> SaveMenuAsync(Guid actor, Guid session, Guid? id, MenuRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockConfigurationAsync(db, ct); await AuthorizeAsync(actor, session, ct);
        var menu = id.HasValue ? await db.NavigationMenus.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AdministrationException("Menu not found.", 404)
            : new NavigationMenu { Id = Guid.NewGuid() };
        if (id is null && await db.NavigationMenus.CountAsync(ct) >= 500) throw new AdministrationException("Menu limit reached.", 409);
        if (await db.Roles.CountAsync(x => request.RoleIds.Contains(x.Id), ct) != request.RoleIds.Length) throw new AdministrationException("One or more roles do not exist.");
        var all = await db.NavigationMenus.AsNoTracking().ToArrayAsync(ct);
        var parents = all.ToDictionary(x => x.Id, x => x.ParentId); parents[menu.Id] = request.ParentId;
        foreach (var node in parents.Keys)
        {
            var seen = new HashSet<Guid>(); Guid? current = node;
            while (current.HasValue)
            {
                if (!seen.Add(current.Value)) throw new AdministrationException("A menu cannot contain a cycle.");
                if (seen.Count > 5) throw new AdministrationException("Menus can have at most five levels.");
                if (!parents.TryGetValue(current.Value, out current)) throw new AdministrationException("Parent menu not found.");
            }
        }
        var before = id.HasValue ? new { menu.LabelEn, menu.LabelBn, menu.Route, menu.ParentId, menu.SortOrder, menu.Enabled, menu.RequiredPermission } : null;
        menu.LabelEn = request.LabelEn.Trim(); menu.LabelBn = request.LabelBn?.Trim(); menu.Icon = request.Icon; menu.Route = request.Route;
        menu.ParentId = request.ParentId; menu.SortOrder = request.SortOrder; menu.Enabled = request.Enabled; menu.RequiredPermission = request.RequiredPermission;
        if (id is null) db.NavigationMenus.Add(menu);
        await db.NavigationMenuRoles.Where(x => x.MenuId == menu.Id).ExecuteDeleteAsync(ct);
        foreach (var role in request.RoleIds) db.NavigationMenuRoles.Add(new NavigationMenuRole { MenuId = menu.Id, RoleId = role });
        Audit(db, actor, id is null ? "Menu.Created" : "Menu.Updated", menu.Id.ToString(), new { Before = before, After = request });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new MenuResponse(menu.Id, menu.LabelEn, menu.LabelBn, menu.Icon, menu.Route, menu.ParentId, menu.SortOrder, menu.Enabled, menu.RequiredPermission, request.RoleIds);
    }
    /// <inheritdoc />
    public async Task DeleteMenuAsync(Guid actor, Guid session, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockConfigurationAsync(db, ct); await AuthorizeAsync(actor, session, ct);
        var menu = await db.NavigationMenus.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AdministrationException("Menu not found.", 404);
        if (await db.NavigationMenus.AnyAsync(x => x.ParentId == id, ct)) throw new AdministrationException("Remove child menus first.", 409);
        db.NavigationMenus.Remove(menu); Audit(db, actor, "Menu.Deleted", id.ToString(), new { menu.LabelEn });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<NavigationNode>> NavigationAsync(Guid userId, bool mfa, CancellationToken ct)
    {
        var rolesForUser = await db.UserRoles.Where(x => x.UserId == userId).Select(x => x.RoleId).ToArrayAsync(ct);
        var grants = mfa ? await permissions.GetAsync(userId, ct) : Array.Empty<string>();
        var all = await MenusAsync(ct);
        var allowed = all.Where(x => x.Enabled && x.RoleIds.Any(rolesForUser.Contains) &&
            (x.RequiredPermission is null || grants.Contains(x.RequiredPermission))).ToArray();
        IReadOnlyCollection<NavigationNode> Build(Guid? parent, int depth)
        {
            if (depth > 5) return Array.Empty<NavigationNode>();
            return allowed.Where(x => x.ParentId == parent).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x =>
                new NavigationNode(x.Id, x.LabelEn, x.LabelBn, x.Icon, x.Route, Build(x.Id, depth + 1)))
                .Where(x => x.Route is not null || x.Children.Count > 0).ToArray();
        }
        return Build(null, 1);
    }
    /// <inheritdoc />
    public async Task<AuditPage> AuditAsync(AuditQuery request, CancellationToken ct)
    {
        var query = db.SecurityAuditEvents.AsNoTracking();
        if (request.ActorId.HasValue) query = query.Where(x => x.ActorId == request.ActorId);
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(x => x.Action == request.Action);
        if (request.From.HasValue) { var from = request.From.Value.UtcDateTime; query = query.Where(x => x.CreatedAtUtc >= from); }
        if (request.To.HasValue) { var to = request.To.Value.UtcDateTime; query = query.Where(x => x.CreatedAtUtc <= to); }
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new AuditResponse(x.Id, x.CreatedAtUtc, x.ActorId, x.Action, x.Target, x.Details)).ToArrayAsync(ct);
        return new AuditPage(items, count, request.Page, request.PageSize);
    }
}
