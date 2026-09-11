using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Authentication;

/// <summary>Prevents stale roles and weaker login methods from bypassing current MFA requirements.</summary>
public sealed class PrivilegedSessionValidator(TheOneDbContext db, UserManager<ApplicationUser> users) : IPrivilegedSessionValidator
{
    /// <inheritdoc />
    public async Task<bool> ValidateAsync(Guid userId, Guid? sessionId, bool mfaClaim,
        IReadOnlyCollection<string> claimedRoles, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return false;
        if (user.StatusChangedAtUtc is not null && (sessionId is null ||
            !await db.RefreshTokens.AsNoTracking().AnyAsync(x => x.UserId == userId && x.SessionId == sessionId &&
                x.SessionCreatedAtUtc > user.StatusChangedAtUtc, cancellationToken))) return false;
        var roles = await users.GetRolesAsync(user);
        var requiresMfa = user.TwoFactorEnabled || LoginSecurityPolicy.IsAdministrator(roles) ||
            LoginSecurityPolicy.IsAdministrator(claimedRoles) || mfaClaim;
        if (!requiresMfa) return true; // Retains ordinary users' existing short-lived JWT behavior.
        if (!user.TwoFactorEnabled || !mfaClaim || sessionId is null || await users.IsLockedOutAsync(user) ||
            AuthenticationSecurity.Fingerprint(roles) != AuthenticationSecurity.Fingerprint(claimedRoles)) return false;
        var fingerprint = AuthenticationSecurity.Fingerprint(roles);
        var now = DateTime.UtcNow;
        return await db.RefreshTokens.AsNoTracking().AnyAsync(x => x.UserId == userId && x.SessionId == sessionId &&
            x.MfaVerified && x.RevokedAtUtc == null && x.ExpiresAtUtc > now &&
            x.SecurityStamp == user.SecurityStamp && x.RolesFingerprint == fingerprint, cancellationToken);
    }
}