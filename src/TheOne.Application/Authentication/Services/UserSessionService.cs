using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Application.Authentication.Services;

/// <summary>Validates profile and session-management use cases.</summary>
public sealed class UserSessionService(IUserSessionStore store) : IUserSessionService
{
    /// <inheritdoc />
    public Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        return store.GetCurrentUserAsync(userId, cancellationToken);
    }
    /// <inheritdoc />
    public Task<SessionListResponse> GetSessionsAsync(Guid userId, Guid? currentSessionId, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        if (pageNumber is < 1 or > 1_000_000 || pageSize is < 1 or > 100)
            throw new AuthenticationException("Page number must be 1–1000000 and page size must be 1–100.");
        return store.GetSessionsAsync(userId, currentSessionId, pageNumber, pageSize, cancellationToken);
    }
    /// <inheritdoc />
    public Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        if (sessionId == Guid.Empty) throw new AuthenticationException("A valid session identifier is required.");
        return store.RevokeSessionAsync(userId, sessionId, cancellationToken);
    }
    /// <inheritdoc />
    public Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        return store.LogoutAllAsync(userId, cancellationToken);
    }
    private static void RequireUser(Guid userId)
    {
        if (userId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
    }
}