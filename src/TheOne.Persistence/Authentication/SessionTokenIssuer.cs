using Microsoft.AspNetCore.Identity;
using TheOne.Application.Abstractions.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Authentication;

/// <summary>Creates access and refresh credentials inside the caller's account transaction.</summary>
public sealed class SessionTokenIssuer(TheOneDbContext db, UserManager<ApplicationUser> users, ITokenService tokens)
{
    /// <summary>Issues a session with its verified authentication strength and current security state.</summary>
    public async Task<TokenResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken,
        bool mfa = false, string method = "pwd", Guid? existingSessionId = null, DateTime? sessionCreatedAtUtc = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roles = (await users.GetRolesAsync(user)).ToArray();
        var sessionId = existingSessionId ?? Guid.NewGuid();
        var access = tokens.GenerateAccessToken(new TokenUser(user.Id, user.Email, user.UserName, roles, sessionId, mfa, method));
        var raw = tokens.GenerateRefreshToken();
        var now = DateTime.UtcNow;
        var expires = now.Add(tokens.RefreshTokenLifetime);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = tokens.HashRefreshToken(raw),
            SessionId = sessionId, SessionCreatedAtUtc = sessionCreatedAtUtc ?? now,
            CreatedAtUtc = now, ExpiresAtUtc = expires, MfaVerified = mfa, AuthenticationMethod = method,
            SecurityStamp = user.SecurityStamp ?? string.Empty, RolesFingerprint = AuthenticationSecurity.Fingerprint(roles)
        });
        TheOne.Persistence.Administration.AdministrationStore.Audit(db, user.Id,
            existingSessionId.HasValue ? "Session.Refreshed" : "Authentication.Login", sessionId.ToString(), new { Method = method, MfaVerified = mfa });
        return new TokenResponse(access.AccessToken, raw, access.ExpiresAtUtc, expires);
    }
}