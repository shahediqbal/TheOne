namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains input for ResetPassword.</summary>
public sealed class ResetPasswordRequest
{
    /// <summary>Gets or sets ChallengeId.</summary>
    public Guid ChallengeId { get; set; }
    /// <summary>Gets or sets Code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets NewPassword.</summary>
    public string NewPassword { get; set; } = string.Empty;
    /// <summary>Gets or sets ConfirmPassword.</summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}