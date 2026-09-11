namespace TheOne.Application.Authentication.DTOs;

/// <summary>Describes an active refresh session without exposing any token or token hash.</summary>
public sealed record SessionResponse(Guid SessionId, DateTime CreatedAtUtc,
    DateTime LastRefreshedAtUtc, DateTime ExpiresAtUtc, bool IsCurrent);

/// <summary>Contains one page of active sessions, ordered newest first.</summary>
public sealed record SessionListResponse(IReadOnlyCollection<SessionResponse> Items,
    int PageNumber, int PageSize, int TotalCount);