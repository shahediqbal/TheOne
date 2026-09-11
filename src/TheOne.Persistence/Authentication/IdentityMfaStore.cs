using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Authentication;

/// <summary>Handles password-proven TOTP enrollment, MFA login, and protected recovery operations.</summary>
public sealed class IdentityMfaStore(TheOneDbContext db, UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, SessionTokenIssuer issuer, IAuthenticatorQrCodeGenerator qr,
    ILogger<IdentityMfaStore> logger) : IMfaStore
{
    /// <inheritdoc />
    public async Task<LoginResponse> BeginEnrollmentAsync(Guid userId, PasswordConfirmationRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await AuthenticationSecurity.LockAsync(db, userId, ct);
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw AuthenticationSecurity.Invalid();
        if (!await AuthenticationSecurity.PasswordAsync(users, signIn, user, request.Password))
        {
            await tx.CommitAsync(ct);
            throw AuthenticationSecurity.Invalid();
        }
        if (user.TwoFactorEnabled || LoginSecurityPolicy.IsAdministrator(await users.GetRolesAsync(user)))
            throw new AuthenticationException("Enroll before administrator promotion; existing authenticators require the protected reset flow.");
        RequireVerifiedContact(user);
        var response = await AuthenticationSecurity.ChallengeAsync(db, user, MfaChallengePurpose.Enrollment, ct);
        await tx.CommitAsync(ct);
        return response;
    }

    /// <inheritdoc />
    public Task<AuthenticatorSetupResponse> GetSetupAsync(MfaChallengeRequest request, CancellationToken ct) =>
        WithChallengeAsync(request.ChallengeId, MfaChallengePurpose.Enrollment, async (user, challenge) =>
        {
            if (user.TwoFactorEnabled) throw AuthenticationSecurity.Invalid();
            RequireVerifiedContact(user);
            var key = await users.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                AuthenticationSecurity.Ensure(await users.ResetAuthenticatorKeyAsync(user));
                key = await users.GetAuthenticatorKeyAsync(user);
                challenge.SecurityStamp = user.SecurityStamp ?? string.Empty;
            }
            if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("Authenticator key creation failed.");
            var label = Uri.EscapeDataString("The One:" + (user.Email ?? user.Id.ToString()));
            var uri = $"otpauth://totp/{label}?secret={Uri.EscapeDataString(key)}&issuer=The%20One&digits=6&period=30";
            return new AuthenticatorSetupResponse(key, uri, qr.Generate(uri));
        }, ct);

    /// <inheritdoc />
    public Task<RecoveryCodesResponse> ConfirmSetupAsync(MfaCodeRequest request, CancellationToken ct) =>
        WithChallengeAsync(request.ChallengeId, MfaChallengePurpose.Enrollment, async (user, challenge) =>
        {
            if (user.TwoFactorEnabled) throw AuthenticationSecurity.Invalid();
            RequireVerifiedContact(user);
            if (!await VerifyAuthenticatorAsync(user, request.Code, ct)) await FailAsync(user, challenge);
            AuthenticationSecurity.Ensure(await users.SetTwoFactorEnabledAsync(user, true));
            AuthenticationSecurity.Ensure(await users.UpdateSecurityStampAsync(user));
            var codes = await NewRecoveryCodesAsync(user);
            challenge.ConsumedAtUtc = DateTime.UtcNow;
            await AuthenticationSecurity.RevokeAsync(db, user.Id, "Authenticator enrolled.", ct);
            logger.LogInformation("Authenticator enrolled for {UserId}", user.Id);
            return codes;
        }, ct);

    /// <inheritdoc />
    public Task<TokenResponse> CompleteLoginAsync(MfaCodeRequest request, CancellationToken ct) =>
        CompleteAsync(request.ChallengeId, request.Code, false, ct);

    /// <inheritdoc />
    public Task<TokenResponse> CompleteRecoveryLoginAsync(MfaRecoveryRequest request, CancellationToken ct) =>
        CompleteAsync(request.ChallengeId, request.RecoveryCode, true, ct);

    private Task<TokenResponse> CompleteAsync(Guid id, string code, bool recovery, CancellationToken ct) =>
        WithChallengeAsync(id, MfaChallengePurpose.Login, async (user, challenge) =>
        {
            if (!user.TwoFactorEnabled) throw AuthenticationSecurity.Invalid();
            var valid = recovery
                ? (await users.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded
                : await VerifyAuthenticatorAsync(user, code, ct);
            if (!valid) await FailAsync(user, challenge);
            AuthenticationSecurity.Ensure(await users.ResetAccessFailedCountAsync(user));
            challenge.ConsumedAtUtc = DateTime.UtcNow;
            var tokens = await issuer.IssueAsync(user, ct, true, recovery ? "pwd+recovery" : "pwd+totp");
            logger.LogInformation("MFA login completed for {UserId}", user.Id);
            return tokens;
        }, ct);

    /// <inheritdoc />
    public async Task<LoginResponse> ResetAsync(Guid userId, AuthenticatorResetRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await AuthenticationSecurity.LockAsync(db, userId, ct);
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw AuthenticationSecurity.Invalid();
        if (!await VerifyManagementCredentialsAsync(user, request, ct))
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            throw AuthenticationSecurity.Invalid();
        }
        RequireVerifiedContact(user);
        AuthenticationSecurity.Ensure(await users.SetTwoFactorEnabledAsync(user, false));
        AuthenticationSecurity.Ensure(await users.ResetAuthenticatorKeyAsync(user));
        await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
        await AuthenticationSecurity.RevokeAsync(db, user.Id, "Authenticator reset.", ct);
        var result = await AuthenticationSecurity.ChallengeAsync(db, user, MfaChallengePurpose.Enrollment, ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Authenticator reset; re-enrollment required for {UserId}", user.Id);
        return result;
    }

    /// <inheritdoc />
    public async Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(Guid userId, AuthenticatorResetRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await AuthenticationSecurity.LockAsync(db, userId, ct);
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw AuthenticationSecurity.Invalid();
        if (!await VerifyManagementCredentialsAsync(user, request, ct))
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            throw AuthenticationSecurity.Invalid();
        }
        var codes = await NewRecoveryCodesAsync(user);
        AuthenticationSecurity.Ensure(await users.UpdateSecurityStampAsync(user));
        await AuthenticationSecurity.RevokeAsync(db, user.Id, "Recovery codes regenerated.", ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Recovery codes regenerated for {UserId}", user.Id);
        return codes;
    }

    private async Task<bool> VerifyManagementCredentialsAsync(ApplicationUser user, AuthenticatorResetRequest request, CancellationToken ct)
    {
        if (!user.TwoFactorEnabled || !await AuthenticationSecurity.PasswordAsync(users, signIn, user, request.Password)) return false;
        var valid = !string.IsNullOrWhiteSpace(request.RecoveryCode)
            ? (await users.RedeemTwoFactorRecoveryCodeAsync(user, request.RecoveryCode)).Succeeded
            : await VerifyAuthenticatorAsync(user, request.Code, ct);
        if (!valid) AuthenticationSecurity.Ensure(await users.AccessFailedAsync(user));
        else AuthenticationSecurity.Ensure(await users.ResetAccessFailedCountAsync(user));
        return valid;
    }

    private async Task<bool> VerifyAuthenticatorAsync(ApplicationUser user, string code, CancellationToken ct)
    {
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code)) return false;
        var now = DateTime.UtcNow;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        var used = await db.UsedAuthenticatorCodes.SingleOrDefaultAsync(x => x.UserId == user.Id && x.CodeHash == hash, ct);
        if (used is not null && used.ExpiresAtUtc > now) return false;
        if (used is null) db.UsedAuthenticatorCodes.Add(new UsedAuthenticatorCode
            { UserId = user.Id, CodeHash = hash, ExpiresAtUtc = now.AddMinutes(5) });
        else used.ExpiresAtUtc = now.AddMinutes(5);
        return true;
    }

    private async Task<T> WithChallengeAsync<T>(Guid id, MfaChallengePurpose purpose,
        Func<ApplicationUser, MfaChallenge, Task<T>> operation, CancellationToken ct)
    {
        var owner = await db.MfaChallenges.AsNoTracking().Where(x => x.Id == id && x.Purpose == purpose)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) throw AuthenticationSecurity.Invalid();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await AuthenticationSecurity.LockAsync(db, owner.Value, ct);
        var user = await users.FindByIdAsync(owner.Value.ToString());
        var challenge = await db.MfaChallenges.SingleAsync(x => x.Id == id, ct);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user) || !await signIn.CanSignInAsync(user) ||
            challenge.ConsumedAtUtc is not null || challenge.ExpiresAtUtc <= DateTime.UtcNow ||
            challenge.FailedAttempts >= 5 || challenge.SecurityStamp != user.SecurityStamp)
            throw AuthenticationSecurity.Invalid();
        try
        {
            var result = await operation(user, challenge);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch (VerificationFailureException)
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            throw AuthenticationSecurity.Invalid();
        }
    }

    private async Task FailAsync(ApplicationUser user, MfaChallenge challenge)
    {
        challenge.FailedAttempts++;
        if (challenge.FailedAttempts >= 5) challenge.ConsumedAtUtc = DateTime.UtcNow;
        AuthenticationSecurity.Ensure(await users.AccessFailedAsync(user));
        throw new VerificationFailureException();
    }
    private async Task<RecoveryCodesResponse> NewRecoveryCodesAsync(ApplicationUser user) =>
        new((await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))?.ToArray()
            ?? throw new InvalidOperationException("Recovery code generation failed."));
    private static void RequireVerifiedContact(ApplicationUser user)
    {
        if (!user.PhoneNumberConfirmed && !user.EmailConfirmed)
            throw new AuthenticationException("Verify an email address or mobile number before authenticator enrollment.");
    }
    private sealed class VerificationFailureException : Exception { }
}