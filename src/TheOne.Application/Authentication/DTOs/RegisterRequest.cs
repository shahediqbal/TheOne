using System.ComponentModel.DataAnnotations;

namespace TheOne.Application.Authentication.DTOs;

/// <summary>
/// Represents user registration information submitted by the client.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// Gets or sets user's full name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets user's mobile number.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string MobileNumber { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets user's password.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets password confirmation.
    /// </summary>
    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}