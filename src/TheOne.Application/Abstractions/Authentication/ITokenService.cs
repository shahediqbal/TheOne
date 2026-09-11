namespace TheOne.Application.Abstractions.Authentication;

/// <summary>
/// Defines security-token generation operations.
/// </summary>
public interface ITokenService
{
    /// <summary>Gets the configured refresh-session lifetime.</summary>
    TimeSpan RefreshTokenLifetime { get; }

    /// <summary>
    /// Generates a signed JWT access token for an authenticated user.
    /// </summary>
    /// <param name="user">
    /// Authenticated user information used to construct token claims.
    /// </param>
    /// <returns>
    /// The generated access token and expiration timestamp.
    /// </returns>
    AccessTokenResult GenerateAccessToken(TokenUser user);

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>A raw refresh token intended to be returned to the client.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Generates a secure SHA-256 hash for a refresh token.
    /// </summary>
    string HashRefreshToken(string refreshToken);
}

/// <summary>
/// Represents a generated JWT access token.
/// </summary>
public sealed record AccessTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);
