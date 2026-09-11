namespace TheOne.Domain.Entities;

/// <summary>
/// Represents a persisted refresh token used to obtain new access tokens
/// without requiring the user to authenticate again.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique refresh token record identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user to whom this token belongs.
    /// </summary>
    public Guid UserId { get; set; }
    /// <summary>Indicates that password and a second factor were verified for this session.</summary>
    public bool MfaVerified { get; set; }
    /// <summary>Gets or sets the verified authentication method.</summary>
    public string AuthenticationMethod { get; set; } = "pwd";
    /// <summary>Gets or sets the security stamp when the session was issued.</summary>
    public string SecurityStamp { get; set; } = string.Empty;
    /// <summary>Gets or sets a digest of the role assignments at session issuance.</summary>
    public string RolesFingerprint { get; set; } = string.Empty;

    /// <summary>Gets or sets the stable login-session identifier retained during rotation.</summary>
    public Guid SessionId { get; set; }

    /// <summary>Gets or sets when this login session began.</summary>
    public DateTime SessionCreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the refresh token.
    /// The raw refresh token must never be persisted.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time when the refresh token was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration time of the refresh token.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the token was revoked.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the token was created.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the token was revoked.
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Gets or sets the reason the token was revoked.
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Gets or sets the token hash that replaced this token during rotation.
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// Indicates whether the refresh token is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    /// <summary>
    /// Indicates whether the refresh token is currently usable.
    /// </summary>
    public bool IsActive => RevokedAtUtc is null && !IsExpired;
}
