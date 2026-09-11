namespace TheOne.Application.UserManagement;

/// <summary>Filters a bounded page of accounts.</summary>
public sealed class UserListRequest
{
    /// <summary>One-based page number.</summary>
    public int Page { get; set; } = 1;
    /// <summary>Maximum accounts returned, up to 100.</summary>
    public int PageSize { get; set; } = 20;
    /// <summary>Literal substring of name, email, or phone.</summary>
    public string? Search { get; set; }
    /// <summary>Optional active-state filter.</summary>
    public bool? IsActive { get; set; }
    /// <summary>Optional exact role name.</summary>
    public string? Role { get; set; }
}

/// <summary>Explicit account status change. Missing status is invalid.</summary>
public sealed class ChangeUserStatusRequest
{
    /// <summary>The requested account status.</summary>
    public bool? IsActive { get; set; }
}

/// <summary>Administrative account information, excluding credentials and token material.</summary>
public sealed record ManagedUserResponse(Guid Id, string? FullName, string? Email, string? MobileNumber,
    bool IsActive, bool EmailVerified, bool MobileVerified, bool AuthenticatorEnabled,
    DateTime CreatedAtUtc, IReadOnlyCollection<string> Roles);

/// <summary>A bounded account page.</summary>
public sealed record UserPageResponse(IReadOnlyCollection<ManagedUserResponse> Items, int TotalCount, int Page, int PageSize);

/// <summary>Validated administrative account operations.</summary>
public interface IUserManagementService
{
    /// <summary>Finds accounts matching the filters.</summary>
    Task<UserPageResponse> ListAsync(UserListRequest request, CancellationToken ct);
    /// <summary>Reads one account, or null when absent.</summary>
    Task<ManagedUserResponse?> GetAsync(Guid userId, CancellationToken ct);
    /// <summary>Changes status under current actor and target security checks.</summary>
    Task<bool> ChangeStatusAsync(Guid actorId, Guid sessionId, Guid userId, ChangeUserStatusRequest request, CancellationToken ct);
}

/// <summary>Persistence boundary for administrative account operations.</summary>
public interface IUserManagementStore
{
    /// <summary>Queries accounts without loading credential fields into responses.</summary>
    Task<UserPageResponse> ListAsync(UserListRequest request, CancellationToken ct);
    /// <summary>Reads a single account.</summary>
    Task<ManagedUserResponse?> GetAsync(Guid userId, CancellationToken ct);
    /// <summary>Changes status atomically; returns false for an absent target.</summary>
    Task<bool> ChangeStatusAsync(Guid actorId, Guid sessionId, Guid userId, bool isActive, CancellationToken ct);
}

/// <summary>Signals that an administrator cannot manage the requested account.</summary>
public sealed class UserManagementForbiddenException : Exception
{
    /// <summary>Creates an access-denied error without revealing account secrets.</summary>
    public UserManagementForbiddenException() : base("You cannot change this account's status.") { }
}
