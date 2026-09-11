namespace TheOne.Domain.Entities;

/// <summary>Separates password-proven enrollment from password-proven MFA login.</summary>
public enum MfaChallengePurpose { Login = 1, Enrollment = 2 }

/// <summary>Represents an expiring, limited-attempt password proof; never usable as an access token.</summary>
public sealed class MfaChallenge
{
    /// <summary>Gets or sets the unpredictable challenge identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the account.</summary>
    public Guid UserId { get; set; }
    /// <summary>Gets or sets the allowed next operation.</summary>
    public MfaChallengePurpose Purpose { get; set; }
    /// <summary>Gets or sets the security stamp at password verification.</summary>
    public string SecurityStamp { get; set; } = string.Empty;
    /// <summary>Gets or sets creation time.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>Gets or sets expiry.</summary>
    public DateTime ExpiresAtUtc { get; set; }
    /// <summary>Gets or sets failed verification attempts.</summary>
    public int FailedAttempts { get; set; }
    /// <summary>Gets or sets consumption or invalidation time.</summary>
    public DateTime? ConsumedAtUtc { get; set; }
}

/// <summary>Prevents reuse of a successfully verified authenticator code during its acceptance window.</summary>
public sealed class UsedAuthenticatorCode
{
    /// <summary>Gets or sets the account.</summary>
    public Guid UserId { get; set; }
    /// <summary>Gets or sets the digest of a previously consumed code.</summary>
    public string CodeHash { get; set; } = string.Empty;
    /// <summary>Gets or sets the end of replay protection for the code.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}