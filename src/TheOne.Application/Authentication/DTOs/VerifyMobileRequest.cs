namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains input for VerifyMobile.</summary>
public sealed class VerifyMobileRequest
{
    /// <summary>Gets or sets ChallengeId.</summary>
    public Guid ChallengeId { get; set; }
    /// <summary>Gets or sets Code.</summary>
    public string Code { get; set; } = string.Empty;
}