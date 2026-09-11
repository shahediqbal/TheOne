using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TheOne.Persistence.Data;

namespace TheOne.Persistence.Identity;

/// <summary>Encrypts authenticator keys and hashes recovery codes in the Identity token store.</summary>
public sealed class ProtectedIdentityUserStore(TheOneDbContext context, IDataProtectionProvider protection,
    IdentityErrorDescriber describer) : UserStore<ApplicationUser, IdentityRole<Guid>, TheOneDbContext, Guid>(context, describer)
{
    /// <inheritdoc />
    public override Task SetAuthenticatorKeyAsync(ApplicationUser user, string key, CancellationToken cancellationToken) =>
        base.SetAuthenticatorKeyAsync(user, "dp:v1:" + Protector(user).Protect(key), cancellationToken);
    /// <inheritdoc />
    public override async Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var stored = await base.GetAuthenticatorKeyAsync(user, cancellationToken);
        if (stored is null) return null;
        // Existing Identity keys remain readable; newly set keys are always encrypted.
        return stored.StartsWith("dp:v1:", StringComparison.Ordinal) ? Protector(user).Unprotect(stored[6..]) : stored;
    }
    /// <inheritdoc />
    public override Task ReplaceCodesAsync(ApplicationUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken) =>
        base.ReplaceCodesAsync(user, recoveryCodes.Select(Hash), cancellationToken);
    /// <inheritdoc />
    public override async Task<bool> RedeemCodeAsync(ApplicationUser user, string code, CancellationToken cancellationToken)
    {
        // Identity's default redemption calls virtual ReplaceCodesAsync with the remaining
        // stored values. Those values are already hashes and must not be hashed again.
        var stored = await GetTokenAsync(user, "[AspNetUserStore]", "RecoveryCodes", cancellationToken);
        if (string.IsNullOrEmpty(stored)) return false;
        var hashes = stored.Split(';').ToList();
        if (!hashes.Remove(Hash(code))) return false;
        await base.ReplaceCodesAsync(user, hashes, cancellationToken);
        return true;
    }
    private IDataProtector Protector(ApplicationUser user) =>
        protection.CreateProtector("TheOne.AuthenticatorKey.v1", user.Id.ToString("N"));
    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
