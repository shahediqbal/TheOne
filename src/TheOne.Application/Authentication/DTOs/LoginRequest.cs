namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains input for Login.</summary>
public sealed class LoginRequest
{
    /// <summary>The registered email address or mobile number.</summary>
    public string UserNameOrMobile { get; set; } = string.Empty;
    /// <summary>The account password.</summary>
    public string Password { get; set; } = string.Empty;
}