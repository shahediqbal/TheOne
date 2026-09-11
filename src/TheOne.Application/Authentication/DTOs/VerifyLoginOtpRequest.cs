namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains VerifyLoginOtp input.</summary>
public sealed class VerifyLoginOtpRequest
{
    /// <summary>Gets or sets ChallengeId.</summary>
    public Guid ChallengeId { get; set; }
    /// <summary>Gets or sets Code.</summary>
    public string Code { get; set; } = string.Empty;
}