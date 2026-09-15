using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheOne.Application.Authentication;
using TheOne.Application.Operations;
using TheOne.Domain.Entities;
using TheOne.Persistence.Administration;
using TheOne.Persistence.Authentication;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;
namespace TheOne.Persistence.Operations;

/// <summary>Local operator commands; no HTTP route or API DI registration exposes these operations.</summary>
public sealed class OperatorAccountService(TheOneDbContext db, UserManager<ApplicationUser> users) : IOperatorAccountService
{
    private async Task<ApplicationUser> TargetAsync(Guid id, string email, CancellationToken ct)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(email)) throw new OperatorException("User ID and expected email are required.");
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new OperatorException("Selected account was not found.");
        await db.Entry(user).ReloadAsync(ct);
        if (!string.Equals(user.NormalizedEmail, users.NormalizeEmail(email.Trim()), StringComparison.Ordinal))
            throw new OperatorException("The user ID and expected email do not identify the same account.");
        return user;
    }
    private Task<int> SuperAdminCountAsync(CancellationToken ct) =>
        (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
         where r.NormalizedName == "SUPERADMIN" || r.NormalizedName == "SUPER ADMIN"
         select ur.UserId).Distinct().CountAsync(ct);

    /// <inheritdoc />
    public async Task<OperatorInspection> InspectAsync(Guid userId, string expectedEmail, CancellationToken ct)
    {
        var user = await TargetAsync(userId, expectedEmail, ct);
        var readable = false;
        try { readable = !string.IsNullOrEmpty(await users.GetAuthenticatorKeyAsync(user)); }
        catch (CryptographicException) { /* Inspection reports unavailable key material without exposing it. */ }
        return new(user.Id, user.Email!, user.IsActive, user.EmailConfirmed || user.PhoneNumberConfirmed, user.TwoFactorEnabled,
            readable, await users.IsLockedOutAsync(user), LoginSecurityPolicy.IsAdministrator(await users.GetRolesAsync(user)), await SuperAdminCountAsync(ct));
    }
    /// <inheritdoc />
    public async Task<BootstrapResult> BootstrapAsync(OperatorRequest request, CancellationToken ct)
    {
        ValidateApproval(request);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await AdministrationStore.LockConfigurationAsync(db, ct);
        await AuthenticationSecurity.LockAsync(db, request.UserId, ct);
        var user = await TargetAsync(request.UserId, request.ExpectedEmail, ct);
        if (await SuperAdminCountAsync(ct) != 0) throw new OperatorException("A SuperAdmin already exists. Bootstrap is unavailable; use normal role administration.");
        RequireEligible(user);
        if (await users.IsLockedOutAsync(user)) throw new OperatorException("A locked account cannot be bootstrapped.");
        if (!user.TwoFactorEnabled) throw new OperatorException("Complete authenticator enrollment before bootstrap.");
        try
        {
            if (string.IsNullOrEmpty(await users.GetAuthenticatorKeyAsync(user))) throw new OperatorException("Authenticator key is missing.");
        }
        catch (CryptographicException) { throw new OperatorException("Authenticator key cannot be read. Use the API's Data Protection key ring and application name."); }
        if (!await db.Roles.AnyAsync(x => x.NormalizedName == "SUPERADMIN", ct)) throw new OperatorException("Apply the application migrations before bootstrap.");
        AuthenticationSecurity.Ensure(await users.AddToRoleAsync(user, LoginSecurityPolicy.SuperAdmin));
        AuthenticationSecurity.Ensure(await users.UpdateSecurityStampAsync(user));
        var revoked = await InvalidateAsync(user.Id, ct);
        AdministrationStore.Audit(db, null, "Operator.SuperAdminBootstrapped", user.Id.ToString(),
            new { request.Operator, request.ApprovedBy, request.Ticket, request.IdentityVerified, RevokedSessions = revoked });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(user.Id, revoked);
    }
    /// <inheritdoc />
    public async Task<OperatorRecoveryResult> RecoverAsync(OperatorRequest request, string newPassword, CancellationToken ct)
    {
        ValidateApproval(request);
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 12 || newPassword.Length > 128)
            throw new OperatorException("The new password must contain between 12 and 128 characters.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await AdministrationStore.LockConfigurationAsync(db, ct);
        await AuthenticationSecurity.LockAsync(db, request.UserId, ct);
        var user = await TargetAsync(request.UserId, request.ExpectedEmail, ct);
        RequireEligible(user);
        if (!LoginSecurityPolicy.IsAdministrator(await users.GetRolesAsync(user)))
            throw new OperatorException("This recovery command is limited to Admin and SuperAdmin accounts.");
        // Identity verifies password policy and hashes the replacement; no direct password/hash SQL updates.
        var resetToken = await users.GeneratePasswordResetTokenAsync(user);
        AuthenticationSecurity.Ensure(await users.ResetPasswordAsync(user, resetToken, newPassword));
        AuthenticationSecurity.Ensure(await users.SetLockoutEndDateAsync(user, null));
        AuthenticationSecurity.Ensure(await users.ResetAccessFailedCountAsync(user));
        AuthenticationSecurity.Ensure(await users.SetTwoFactorEnabledAsync(user, false));
        AuthenticationSecurity.Ensure(await users.ResetAuthenticatorKeyAsync(user));
        if (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 0) is null)
            throw new OperatorException("Recovery-code invalidation failed.");
        AuthenticationSecurity.Ensure(await users.UpdateSecurityStampAsync(user));
        var revoked = await InvalidateAsync(user.Id, ct);
        var challenge = await AuthenticationSecurity.ChallengeAsync(db, user, MfaChallengePurpose.Enrollment, ct);
        AdministrationStore.Audit(db, null, "Operator.AdminRecoveryStarted", user.Id.ToString(),
            new { request.Operator, request.ApprovedBy, request.Ticket, request.IdentityVerified, RevokedSessions = revoked, RequiresAuthenticatorEnrollment = true });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(user.Id, challenge.ChallengeId!.Value, 300, revoked);
    }

    private async Task<int> InvalidateAsync(Guid userId, CancellationToken ct)
    {
        var revoked = await AuthenticationSecurity.RevokeAsync(db, userId, "Operator account security operation.", ct);
        var now = DateTime.UtcNow;
        await db.MfaChallenges.Where(x => x.UserId == userId && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), ct);
        await db.OtpChallenges.Where(x => x.UserId == userId && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), ct);
        return revoked;
    }
    private static void RequireEligible(ApplicationUser user)
    {
        if (!user.IsActive) throw new OperatorException("Inactive accounts cannot be recovered or bootstrapped by this command.");
        if (!user.EmailConfirmed && !user.PhoneNumberConfirmed) throw new OperatorException("The account must already have a verified contact.");
    }
    private static void ValidateApproval(OperatorRequest request)
    {
        static bool Valid(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 100 && !value.Any(char.IsControl);
        if (!Valid(request.Operator) || !Valid(request.ApprovedBy) || !Valid(request.Ticket))
            throw new OperatorException("Operator, approver and approval ticket are required (maximum 100 characters each).");
        if (string.Equals(request.Operator.Trim(), request.ApprovedBy.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new OperatorException("A different person must approve this operation.");
        if (!request.IdentityVerified) throw new OperatorException("Record independent identity verification before applying this operation.");
    }
}
