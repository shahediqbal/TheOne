namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains PasswordConfirmation input.</summary>
public sealed class PasswordConfirmationRequest
{
    /// <summary>Gets or sets Password.</summary>
    public string Password { get; set; } = string.Empty;
}