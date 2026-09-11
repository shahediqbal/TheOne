namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains a JWT and the rotating refresh token for a session.</summary>
public sealed record TokenResponse(string AccessToken, string RefreshToken,
    DateTime ExpiresAtUtc, DateTime RefreshTokenExpiresAtUtc);