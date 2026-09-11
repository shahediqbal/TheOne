namespace TheOne.Application.Authentication;

/// <summary>Defines canonical roles and mandatory administrator MFA rules.</summary>
public static class LoginSecurityPolicy
{
    /// <summary>Default registered-user role.</summary>
    public const string Member = "Member";
    /// <summary>Administrator role.</summary>
    public const string Admin = "Admin";
    /// <summary>Highest administrator role.</summary>
    public const string SuperAdmin = "SuperAdmin";
    /// <summary>Requires MFA when any administrator role is present, regardless of other roles.</summary>
    public static bool IsAdministrator(IEnumerable<string> roles) => roles.Any(role =>
        string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, "Super Admin", StringComparison.OrdinalIgnoreCase));
}