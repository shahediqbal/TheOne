using System.Globalization;
using System.Security.Cryptography;
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

/// <summary>Implements single-use mobile verification and password recovery with PostgreSQL row locks.</summary>
public sealed class IdentityAccountRecoveryStore(TheOneDbContext db, UserManager<ApplicationUser> users,
    ISmsSender sms, IOtpCodeProtector protector, TimeProvider clock, SessionTokenIssuer issuer,
    SignInManager<ApplicationUser> signIn,
    ILogger<IdentityAccountRecoveryStore> logger) : IAccountRecoveryStore
{
    /// <inheritdoc />
    public Task<OtpChallengeResponse> RequestMobileVerificationAsync(Guid userId, CancellationToken cancellationToken = default) =>
        IssueAsync(userId, OtpPurpose.MobileVerification, cancellationToken);

    /// <inheritdoc />
    public async Task<OtpChallengeResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        sms.EnsureAvailable(); // Identical disabled-provider response for existing and unknown accounts.
        var identifier = request.UserNameOrMobile.Trim();
        var user = identifier.Contains('@') ? await users.FindByEmailAsync(identifier)
            : await db.Users.SingleOrDefaultAsync(x => x.PhoneNumber == identifier, cancellationToken);
        if (user is null) return new OtpChallengeResponse(Guid.NewGuid());
        return await IssueAsync(user.Id, OtpPurpose.PasswordReset, cancellationToken);
    }

    private async Task<OtpChallengeResponse> IssueAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        sms.EnsureAvailable();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(userId, cancellationToken);
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is not null) await db.Entry(user).ReloadAsync(cancellationToken);
        var recovery = purpose != OtpPurpose.MobileVerification;
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.PhoneNumber) ||
            (recovery && (!user.PhoneNumberConfirmed || user.TwoFactorEnabled ||
                LoginSecurityPolicy.IsAdministrator(await users.GetRolesAsync(user)))) ||
            (purpose == OtpPurpose.Login && (await users.IsLockedOutAsync(user!) || !await signIn.CanSignInAsync(user!))))
        {
            if (recovery) return new OtpChallengeResponse(Guid.NewGuid());
            throw new AuthenticationException("This account cannot request mobile verification.");
        }
        if (!recovery && user.PhoneNumberConfirmed)
            throw new AuthenticationException("The mobile number is already verified.");

        var now = clock.GetUtcNow().UtcDateTime;
        var recent = db.OtpChallenges.Where(x => x.UserId == userId && x.CreatedAtUtc > now.AddHours(-1));
        if (await recent.CountAsync(cancellationToken) >= 5 ||
            await recent.AnyAsync(x => x.CreatedAtUtc > now.AddSeconds(-60), cancellationToken))
        {
            if (recovery) return new OtpChallengeResponse(Guid.NewGuid());
            throw new OtpRequestLimitException();
        }

        await db.OtpChallenges.Where(x => x.UserId == userId && x.Purpose == purpose && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), cancellationToken);
        var id = Guid.NewGuid();
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new OtpChallenge
        {
            Id = id, UserId = userId, Purpose = purpose, MobileNumber = user.PhoneNumber,
            SecurityStamp = user.SecurityStamp ?? string.Empty,
            ProtectedCode = protector.Protect(id, code), CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(5)
        };
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);
        // Persist request limits before network I/O, so cancellation cannot roll them back.
        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();
        var failed = false;
        try
        {
            await sms.SendAsync(user.PhoneNumber,
                $"Your The One OTP is {code}", cancellationToken);
        }
        catch (OtpDeliveryException)
        {
            failed = true;
            logger.LogWarning("OTP delivery failed for challenge {ChallengeId}", id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await db.OtpChallenges.Where(x => x.Id == id && x.ConsumedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), CancellationToken.None);
            throw;
        }
        // Complete this persisted attempt even if the caller disconnected after provider acceptance.
        if (failed)
            await db.OtpChallenges.Where(x => x.Id == id && x.ConsumedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), CancellationToken.None);
        else
            await db.OtpChallenges.Where(x => x.Id == id && x.ConsumedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeliveredAtUtc, clock.GetUtcNow().UtcDateTime), CancellationToken.None);
        if (failed && !recovery) throw new OtpDeliveryException();
        return new OtpChallengeResponse(id);
    }

    /// <inheritdoc />
    public async Task VerifyMobileAsync(Guid userId, VerifyMobileRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(userId, cancellationToken);
        var user = await users.FindByIdAsync(userId.ToString());
        var challenge = await db.OtpChallenges.SingleOrDefaultAsync(x =>
            x.Id == request.ChallengeId && x.UserId == userId && x.Purpose == OtpPurpose.MobileVerification, cancellationToken);
        if (!Validate(user, challenge, request.Code))
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw InvalidCode();
        }

        user!.PhoneNumberConfirmed = true;
        EnsureSuccess(await users.UpdateAsync(user));
        challenge!.ConsumedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Mobile verified for account {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var owner = await db.OtpChallenges.AsNoTracking()
            .Where(x => x.Id == request.ChallengeId && x.Purpose == OtpPurpose.PasswordReset)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken);
        if (owner is null) throw InvalidCode();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(owner.Value, cancellationToken);
        var user = await users.FindByIdAsync(owner.Value.ToString());
        var challenge = await db.OtpChallenges.SingleAsync(x => x.Id == request.ChallengeId, cancellationToken);
        if (user is null || !user.PhoneNumberConfirmed || user.TwoFactorEnabled ||
            LoginSecurityPolicy.IsAdministrator(await users.GetRolesAsync(user)) || !Validate(user, challenge, request.Code))
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw InvalidCode();
        }

        // Identity remains responsible for password policy, hashing, and security-stamp rotation.
        var resetToken = await users.GeneratePasswordResetTokenAsync(user);
        EnsureSuccess(await users.ResetPasswordAsync(user, resetToken, request.NewPassword));
        var now = clock.GetUtcNow().UtcDateTime;
        await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevocationReason, "Password reset."), cancellationToken);
        await db.OtpChallenges.Where(x => x.UserId == user.Id && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Password reset and refresh sessions revoked for {UserId}", user.Id);
    }


    /// <inheritdoc />
    public async Task<OtpChallengeResponse> RequestLoginOtpAsync(SmsLoginRequest request, CancellationToken cancellationToken = default)
    {
        sms.EnsureAvailable();
        var mobile = request.MobileNumber.Trim();
        var user = await db.Users.SingleOrDefaultAsync(x => x.PhoneNumber == mobile, cancellationToken);
        if (user is null) return new OtpChallengeResponse(Guid.NewGuid());
        return await IssueAsync(user.Id, OtpPurpose.Login, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TokenResponse> LoginWithOtpAsync(VerifyLoginOtpRequest request, CancellationToken cancellationToken = default)
    {
        var owner = await db.OtpChallenges.AsNoTracking()
            .Where(x => x.Id == request.ChallengeId && x.Purpose == OtpPurpose.Login)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken);
        if (owner is null) throw InvalidCode();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(owner.Value, cancellationToken);
        var user = await users.FindByIdAsync(owner.Value.ToString());
        var challenge = await db.OtpChallenges.SingleAsync(x => x.Id == request.ChallengeId, cancellationToken);
        if (user is null || !user.PhoneNumberConfirmed || user.TwoFactorEnabled ||
            LoginSecurityPolicy.IsAdministrator(await users.GetRolesAsync(user)) ||
            await users.IsLockedOutAsync(user) || !await signIn.CanSignInAsync(user) ||
            !Validate(user, challenge, request.Code))
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw InvalidCode();
        }
        challenge.ConsumedAtUtc = clock.GetUtcNow().UtcDateTime;
        var response = await issuer.IssueAsync(user, cancellationToken, false, "sms");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("SMS login completed for {UserId}", user.Id);
        return response;
    }
    private bool Validate(ApplicationUser? user, OtpChallenge? challenge, string code)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        if (user is null || !user.IsActive || challenge is null || challenge.DeliveredAtUtc is null || challenge.ConsumedAtUtc is not null ||
            challenge.ExpiresAtUtc <= now || challenge.FailedAttempts >= 5 ||
            challenge.MobileNumber != user.PhoneNumber || challenge.SecurityStamp != user.SecurityStamp)
            return false;
        if (protector.Matches(challenge.Id, challenge.ProtectedCode, code)) return true;
        challenge.FailedAttempts++;
        if (challenge.FailedAttempts >= 5) challenge.ConsumedAtUtc = now;
        return false;
    }

    private Task LockUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Id"" = {userId} FOR UPDATE", cancellationToken);

    private static AuthenticationException InvalidCode() => new("Invalid or expired verification code.");
    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new AuthenticationException("The operation could not be completed.",
                errors: result.Errors.Select(x => x.Description).ToArray());
    }
}
