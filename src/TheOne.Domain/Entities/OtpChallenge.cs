namespace TheOne.Domain.Entities;

/// <summary>Identifies the operation authorized by a one-time code.</summary>
public enum OtpPurpose { MobileVerification = 1, PasswordReset = 2, Login = 3 }

/// <summary>Stores a protected, expiring, single-use verification challenge.</summary>
public sealed class OtpChallenge
{
    /// <summary>Gets or sets the unpredictable public challenge identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the owning account.</summary>
    public Guid UserId { get; set; }
    /// <summary>Gets or sets the authorized operation.</summary>
    public OtpPurpose Purpose { get; set; }
    /// <summary>Gets or sets the Data Protection encrypted code.</summary>
    public string ProtectedCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the phone number at issuance.</summary>
    public string MobileNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the Identity security stamp at issuance.</summary>
    public string SecurityStamp { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation time.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>Gets or sets the expiry time.</summary>
    public DateTime ExpiresAtUtc { get; set; }
    /// <summary>Gets or sets when the sender accepted delivery; pending codes cannot be verified.</summary>
    public DateTime? DeliveredAtUtc { get; set; }
    /// <summary>Gets or sets the number of failed verification attempts.</summary>
    public int FailedAttempts { get; set; }
    /// <summary>Gets or sets consumption, supersession, or delivery-failure time.</summary>
    public DateTime? ConsumedAtUtc { get; set; }
}
