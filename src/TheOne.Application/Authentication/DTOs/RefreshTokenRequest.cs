namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains input for RefreshToken.</summary>
public sealed class RefreshTokenRequest
{
    /// <summary>The opaque refresh token. Never log this value.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}