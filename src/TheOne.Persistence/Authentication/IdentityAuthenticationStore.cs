using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheOne.Application.Abstractions.Authentication;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Authentication;

/// <summary>Implements authentication against PostgreSQL and ASP.NET Core Identity.</summary>
public sealed class IdentityAuthenticationStore(
    TheOneDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles,
    SignInManager<ApplicationUser> signIn,
    ITokenService tokens,
    SessionTokenIssuer issuer,
    ILogger<IdentityAuthenticationStore> logger) : IAuthenticationStore
{
    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize registrations through this adapter, including default-role creation and mobile uniqueness.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7210451901)", cancellationToken);
        var email = request.Email.Trim();
        var mobile = request.MobileNumber.Trim();
        if (await users.FindByEmailAsync(email) is not null ||
            await db.Users.AnyAsync(x => x.PhoneNumber == mobile, cancellationToken))
            throw new AuthenticationException("Unable to register with the supplied details.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = email, Email = email,
            FullName = request.FullName.Trim(), PhoneNumber = mobile
        };
        EnsureSuccess(await users.CreateAsync(user, request.Password));
        const string memberRole = "Member";
        if (!await roles.RoleExistsAsync(memberRole))
            EnsureSuccess(await roles.CreateAsync(new IdentityRole<Guid>(memberRole)));
        EnsureSuccess(await users.AddToRoleAsync(user, memberRole));
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Account registered: {UserId}", user.Id);
        return new RegisterResponse { UserId = user.Id, Email = email, Message = "Registration successful." };
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.UserNameOrMobile.Trim();
        var user = identifier.Contains('@')
            ? await users.FindByEmailAsync(identifier)
            : await db.Users.SingleOrDefaultAsync(x => x.PhoneNumber == identifier, cancellationToken);
        if (user is null) throw InvalidCredentials();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(user.Id, cancellationToken);
        await db.Entry(user).ReloadAsync(cancellationToken);
        if (!user.IsActive)
            throw InvalidCredentials();

        var valid = await AuthenticationSecurity.PasswordAsync(users, signIn, user, request.Password);
        if (!valid)
        {
            // Failed-attempt counters must survive an unsuccessful login.
            await transaction.CommitAsync(cancellationToken);
            logger.LogWarning("Password login rejected: {UserId}", user.Id);
            throw InvalidCredentials();
        }
        var administrator = LoginSecurityPolicy.IsAdministrator(await users.GetRolesAsync(user));
        if (administrator && !user.TwoFactorEnabled)
            throw new AuthenticationException("Authenticator enrollment is required before administrator access can be activated.");
        if (user.TwoFactorEnabled)
        {
            var challenge = await AuthenticationSecurity.ChallengeAsync(db, user, MfaChallengePurpose.Login, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return challenge;
        }
        AuthenticationSecurity.Ensure(await users.ResetAccessFailedCountAsync(user));
        var response = await issuer.IssueAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Password login succeeded: {UserId}", user.Id);
        return LoginResponse.Complete(response);
    }

    /// <inheritdoc />
    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var owner = await db.RefreshTokens.AsNoTracking()
            .Where(x => x.TokenHash == hash).Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (owner is null) throw InvalidCredentials();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(owner.Value, cancellationToken);
        var token = await db.RefreshTokens.SingleAsync(x => x.TokenHash == hash, cancellationToken);
        var user = await users.FindByIdAsync(owner.Value.ToString());
        if (token.RevokedAtUtc is not null)
        {
            // Reuse of a rotated token invalidates all sessions for this account.
            if (token.ReplacedByTokenHash is not null)
            {
                await RevokeAllAsync(owner.Value, "Refresh token reuse detected.", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogWarning("Refresh token reuse detected: {UserId}", owner.Value);
            }
            throw InvalidCredentials();
        }
        if (token.IsExpired || user is null || !user.IsActive ||
            await users.IsLockedOutAsync(user) || !await signIn.CanSignInAsync(user))
            throw InvalidCredentials();

        var currentRoles = await users.GetRolesAsync(user);
        var requiresMfa = user.TwoFactorEnabled || LoginSecurityPolicy.IsAdministrator(currentRoles) || token.MfaVerified;
        if (requiresMfa && (!user.TwoFactorEnabled || !token.MfaVerified || token.SecurityStamp != user.SecurityStamp ||
            token.RolesFingerprint != AuthenticationSecurity.Fingerprint(currentRoles))) throw InvalidCredentials();
        var response = await issuer.IssueAsync(user, cancellationToken, token.MfaVerified, token.AuthenticationMethod,
            token.SessionId, token.SessionCreatedAtUtc);
        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevocationReason = "Rotated.";
        token.ReplacedByTokenHash = tokens.HashRefreshToken(response.RefreshToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var owner = await db.RefreshTokens.AsNoTracking()
            .Where(x => x.TokenHash == hash).Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (owner is null) return; // Idempotent without revealing whether a token exists.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(owner.Value, cancellationToken);
        var token = await db.RefreshTokens.SingleAsync(x => x.TokenHash == hash, cancellationToken);
        // Follow replacement links so logout racing with refresh still ends that session.
        while (true)
        {
            token.RevokedAtUtc ??= DateTime.UtcNow;
            token.RevocationReason = "Logged out.";
            if (token.ReplacedByTokenHash is not { } nextHash) break;
            var next = await db.RefreshTokens.SingleOrDefaultAsync(x =>
                x.TokenHash == nextHash && x.UserId == owner.Value, cancellationToken);
            if (next is null) break;
            token = next;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Session logged out: {UserId}", owner.Value);
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(userId, cancellationToken);
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
            throw InvalidCredentials();
        EnsureSuccess(await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));
        await RevokeAllAsync(userId, "Password changed.", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Password changed and refresh sessions revoked: {UserId}", userId);
    }

    private Task LockUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Id"" = {userId} FOR UPDATE", cancellationToken);

    private Task<int> RevokeAllAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevocationReason, reason), cancellationToken);
    }

    private static AuthenticationException InvalidCredentials() =>
        new("Invalid credentials or session.", invalidCredentials: true);

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new AuthenticationException("The authentication operation could not be completed.",
                errors: result.Errors.Select(x => x.Description).ToArray());
    }
}
