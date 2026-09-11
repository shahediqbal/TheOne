using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Authentication;

/// <summary>Shares account locking, password-proof, and session-security rules.</summary>
internal static class AuthenticationSecurity
{
    internal static string Fingerprint(IEnumerable<string> roles) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            roles.Select(x => x.ToUpperInvariant()).OrderBy(x => x, StringComparer.Ordinal).ToArray()))));
    internal static Task LockAsync(TheOneDbContext db, Guid userId, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Id"" = {userId} FOR UPDATE", ct);
    internal static AuthenticationException Invalid() => new("Invalid credentials or verification challenge.", true);
    internal static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new AuthenticationException("The operation could not be completed.",
            errors: result.Errors.Select(x => x.Description).ToArray());
    }
    internal static async Task<bool> PasswordAsync(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        ApplicationUser user, string password)
    {
        if (!user.IsActive || await users.IsLockedOutAsync(user) || !await signIn.CanSignInAsync(user)) return false;
        if (await users.CheckPasswordAsync(user, password)) return true;
        Ensure(await users.AccessFailedAsync(user));
        return false;
    }
    internal static async Task<LoginResponse> ChallengeAsync(TheOneDbContext db, ApplicationUser user,
        MfaChallengePurpose purpose, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.MfaChallenges.Where(x => x.UserId == user.Id && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), ct);
        var challenge = new MfaChallenge
        {
            Id = Guid.NewGuid(), UserId = user.Id, Purpose = purpose,
            SecurityStamp = user.SecurityStamp ?? string.Empty, CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(5)
        };
        db.MfaChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        return LoginResponse.Challenge(challenge.Id, purpose == MfaChallengePurpose.Enrollment);
    }
    internal static Task<int> RevokeAsync(TheOneDbContext db, Guid userId, string reason, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now).SetProperty(x => x.RevocationReason, reason), ct);
    }
}