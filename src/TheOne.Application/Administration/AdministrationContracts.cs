namespace TheOne.Application.Administration;

/// <summary>Server-defined permissions; clients cannot invent new API permissions.</summary>
public static class Permissions
{
    /// <summary>Read account details.</summary>
    public const string UsersRead = "users.read";
    /// <summary>Change eligible account status.</summary>
    public const string UsersManage = "users.manage";
    /// <summary>Read security history.</summary>
    public const string AuditRead = "audit.read";
    /// <summary>Permission claim type.</summary>
    public const string ClaimType = "permission";
    /// <summary>Delegable administrative permissions; role and menu configuration remain SuperAdmin-only.</summary>
    public static readonly IReadOnlyCollection<string> All = Array.AsReadOnly(new[] { UsersRead, UsersManage, AuditRead });
}

/// <summary>Role name and complete permission selection.</summary>
public sealed record RoleRequest(string Name, string[] Permissions);
/// <summary>Role details.</summary>
public sealed record RoleResponse(Guid Id, string Name, bool IsSystem, IReadOnlyCollection<string> Permissions);
/// <summary>Complete menu definition and role visibility assignments.</summary>
public sealed record MenuRequest(string LabelEn, string? LabelBn, string? Icon, string? Route, Guid? ParentId,
    int SortOrder, bool Enabled, string? RequiredPermission, Guid[] RoleIds);
/// <summary>Menu definition returned for administration.</summary>
public sealed record MenuResponse(Guid Id, string LabelEn, string? LabelBn, string? Icon, string? Route, Guid? ParentId,
    int SortOrder, bool Enabled, string? RequiredPermission, IReadOnlyCollection<Guid> RoleIds);
/// <summary>User-visible navigation node.</summary>
public sealed record NavigationNode(Guid Id, string LabelEn, string? LabelBn, string? Icon, string? Route, IReadOnlyCollection<NavigationNode> Children);
/// <summary>Bounded audit-history filters.</summary>
public sealed record AuditQuery(int Page = 1, int PageSize = 20, Guid? ActorId = null, string? Action = null, DateTimeOffset? From = null, DateTimeOffset? To = null);
/// <summary>Safe immutable audit representation.</summary>
public sealed record AuditResponse(Guid Id, DateTime CreatedAtUtc, Guid? ActorId, string Action, string Target, string Details);
/// <summary>Audit result page.</summary>
public sealed record AuditPage(IReadOnlyCollection<AuditResponse> Items, int TotalCount, int Page, int PageSize);
/// <summary>Current database permission lookup.</summary>
public interface IPermissionReader
{
    /// <summary>Reads current grants; no JWT permission cache is used.</summary>
    Task<IReadOnlyCollection<string>> GetAsync(Guid userId, CancellationToken ct);
}
/// <summary>Validated administration use cases.</summary>
public interface IAdministrationService
{
    /// <summary>Lists roles and their grants.</summary>
    Task<IReadOnlyCollection<RoleResponse>> RolesAsync(CancellationToken ct);
    /// <summary>Creates or updates a role.</summary>
    Task<RoleResponse> SaveRoleAsync(Guid actor, Guid session, Guid? id, RoleRequest request, CancellationToken ct);
    /// <summary>Deletes an unused custom role.</summary>
    Task DeleteRoleAsync(Guid actor, Guid session, Guid id, CancellationToken ct);
    /// <summary>Assigns or removes a role, subject to account and MFA protections.</summary>
    Task AssignRoleAsync(Guid actor, Guid session, Guid userId, Guid roleId, bool assign, CancellationToken ct);
    /// <summary>Lists menu definitions.</summary>
    Task<IReadOnlyCollection<MenuResponse>> MenusAsync(CancellationToken ct);
    /// <summary>Creates or updates a menu.</summary>
    Task<MenuResponse> SaveMenuAsync(Guid actor, Guid session, Guid? id, MenuRequest request, CancellationToken ct);
    /// <summary>Deletes a leaf menu.</summary>
    Task DeleteMenuAsync(Guid actor, Guid session, Guid id, CancellationToken ct);
    /// <summary>Builds permitted navigation for the signed-in account.</summary>
    Task<IReadOnlyCollection<NavigationNode>> NavigationAsync(Guid userId, bool mfa, CancellationToken ct);
    /// <summary>Reads filtered audit history.</summary>
    Task<AuditPage> AuditAsync(AuditQuery request, CancellationToken ct);
}
/// <summary>Persistence implementation boundary.</summary>
public interface IAdministrationStore : IAdministrationService { }
/// <summary>Expected administration error with an explicit HTTP status.</summary>
public sealed class AdministrationException(string message, int statusCode = 400) : Exception(message)
{
    /// <summary>HTTP status for the safe error response.</summary>
    public int StatusCode { get; } = statusCode;
}
