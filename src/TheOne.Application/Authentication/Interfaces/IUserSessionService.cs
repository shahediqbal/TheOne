using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Provides the current user's profile and refresh-session management.</summary>
public interface IUserSessionService
{
    /// <summary>Reads current profile and roles from the account store.</summary>
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>Lists active sessions belonging only to this account.</summary>
    Task<SessionListResponse> GetSessionsAsync(Guid userId, Guid? currentSessionId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
    /// <summary>Revokes a session belonging to this account; unknown sessions are ignored.</summary>
    Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    /// <summary>Revokes every existing refresh session belonging to this account.</summary>
    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default);
}