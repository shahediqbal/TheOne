using Microsoft.AspNetCore.Identity;

namespace TheOne.Persistence.Identity;

/// <summary>
/// Represents an authenticated user account within The One platform.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Indicates whether the account is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Rejects sessions created before the most recent administrative status change.</summary>
    public DateTime? StatusChangedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the account was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}