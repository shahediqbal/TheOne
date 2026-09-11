namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains input for ForgotPassword.</summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>Gets or sets UserNameOrMobile.</summary>
    public string UserNameOrMobile { get; set; } = string.Empty;
}