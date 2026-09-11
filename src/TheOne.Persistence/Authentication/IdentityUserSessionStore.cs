using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Authentication;

/// <summary>Reads account profiles and manages refresh sessions using account-level transaction locks.</summary>
public sealed class IdentityUserSessionStore(TheOneDbContext db, UserManager<ApplicationUser> users,
    ILogger<IdentityUserSessionStore> logger) : IUserSessionStore
{
    /// <inheritdoc />
    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var roles = await users.GetRolesAsync(user);
        return new CurrentUserResponse(user.Id, user.FullName, user.Email, user.PhoneNumber,
            user.EmailConfirmed, user.PhoneNumberConfirmed, user.CreatedAtUtc, roles.OrderBy(x => x).ToArray());
    }

    /// <inheritdoc />
    public async Task<SessionListResponse> GetSessionsAsync(Guid userId, Guid? currentSessionId, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default)
    {
        // Keep pagination count and rows consistent with concurrent rotation/revocation.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(userId, cancellationToken);
        await RequireUserAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        var query = db.RefreshTokens.AsNoTracking()
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.SessionCreatedAtUtc).ThenBy(x => x.SessionId)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new SessionResponse(x.SessionId, x.SessionCreatedAtUtc, x.CreatedAtUtc,
                x.ExpiresAtUtc, currentSessionId != null && x.SessionId == currentSessionId))
            .ToArrayAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SessionListResponse(rows, pageNumber, pageSize, count);
    }

    /// <inheritdoc />
    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(userId, cancellationToken);
        await RequireUserAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        var count = await db.RefreshTokens.Where(x => x.UserId == userId && x.SessionId == sessionId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevocationReason, "Session revoked by account owner."), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (count > 0) logger.LogInformation("Session {SessionId} revoked by {UserId}", sessionId, userId);
    }

    /// <inheritdoc />
    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(userId, cancellationToken);
        await RequireUserAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevocationReason, "Logged out all devices."), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("All refresh sessions revoked by {UserId}", userId);
    }

    private async Task<ApplicationUser> RequireUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
            throw new AuthenticationException("Authentication is required.", true);
        return user;
    }

    private Task LockUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Id"" = {userId} FOR UPDATE", cancellationToken);
}