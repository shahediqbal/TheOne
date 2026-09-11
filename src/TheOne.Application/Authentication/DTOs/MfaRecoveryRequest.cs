namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains MfaRecovery input.</summary>
public sealed class MfaRecoveryRequest
{
    /// <summary>Gets or sets ChallengeId.</summary>
    public Guid ChallengeId { get; set; }
    /// <summary>Gets or sets RecoveryCode.</summary>
    public string RecoveryCode { get; set; } = string.Empty;
}