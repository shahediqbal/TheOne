namespace TheOne.Domain.Entities;

/// <summary>Persistent navigation definition; role visibility is separate from API permission.</summary>
public sealed class NavigationMenu
{
    /// <summary>Identity.</summary>
    public Guid Id { get; set; }
    /// <summary>English label.</summary>
    public string LabelEn { get; set; } = "";
    /// <summary>Bangla label.</summary>
    public string? LabelBn { get; set; }
    /// <summary>Icon name, never markup.</summary>
    public string? Icon { get; set; }
    /// <summary>Internal application route.</summary>
    public string? Route { get; set; }
    /// <summary>Optional parent.</summary>
    public Guid? ParentId { get; set; }
    /// <summary>Sibling display order.</summary>
    public int SortOrder { get; set; }
    /// <summary>Availability.</summary>
    public bool Enabled { get; set; }
    /// <summary>Optional additional API permission needed for display.</summary>
    public string? RequiredPermission { get; set; }
}
/// <summary>Role visibility assignment.</summary>
public sealed class NavigationMenuRole
{
    /// <summary>Menu identity.</summary>
    public Guid MenuId { get; set; }
    /// <summary>Identity role identity.</summary>
    public Guid RoleId { get; set; }
}
/// <summary>Append-only application security history; no credentials or request bodies.</summary>
public sealed class SecurityAuditEvent
{
    /// <summary>Identity.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Occurrence in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Actor identifier retained independently of account deletion.</summary>
    public Guid? ActorId { get; set; }
    /// <summary>Stable event name.</summary>
    public string Action { get; set; } = "";
    /// <summary>Target identifier or safe route.</summary>
    public string Target { get; set; } = "";
    /// <summary>Allowlisted non-secret JSON metadata.</summary>
    public string Details { get; set; } = "{}";
}
