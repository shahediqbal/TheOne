namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains AuthenticatorReset input.</summary>
public sealed class AuthenticatorResetRequest
{
    /// <summary>Gets or sets Password.</summary>
    public string Password { get; set; } = string.Empty;
    /// <summary>Gets or sets Code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets RecoveryCode.</summary>
    public string RecoveryCode { get; set; } = string.Empty;
}