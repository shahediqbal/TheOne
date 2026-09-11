using Microsoft.EntityFrameworkCore;
using TheOne.Application.Administration;
using TheOne.Persistence.Data;
namespace TheOne.Persistence.Administration;

/// <summary>Reads effective grants from current role membership, never from stale permission claims.</summary>
public sealed class PermissionReader(TheOneDbContext db) : IPermissionReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetAsync(Guid userId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, ct)) return Array.Empty<string>();
        var roles = db.UserRoles.Where(x => x.UserId == userId);
        if (await roles.AnyAsync(x => db.Roles.Any(r => r.Id == x.RoleId && r.NormalizedName == "SUPERADMIN"), ct)) return Permissions.All;
        return await db.RoleClaims.Where(c => c.ClaimType == Permissions.ClaimType && roles.Any(r => r.RoleId == c.RoleId) && c.ClaimValue != null)
            .Select(c => c.ClaimValue!).Distinct().ToArrayAsync(ct);
    }
}
