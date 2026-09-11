namespace TheOne.Application.Authentication.DTOs;

/// <summary>
/// Represents the response returned after successful registration.
/// </summary>
public sealed class RegisterResponse
{
    /// <summary>
    /// Gets or sets created user identifier.
    /// </summary>
    public Guid UserId { get; set; }


    /// <summary>
    /// Gets or sets registered email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets registration status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}