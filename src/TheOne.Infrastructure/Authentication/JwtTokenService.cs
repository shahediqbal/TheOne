using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TheOne.Application.Abstractions.Authentication;

namespace TheOne.Infrastructure.Authentication;

/// <summary>
/// Generates and hashes authentication tokens used by The One platform.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    /// <inheritdoc />
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_jwtOptions.RefreshTokenExpirationDays);

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    /// <inheritdoc />
    public AccessTokenResult GenerateAccessToken(TokenUser user)
    {
        var now = DateTime.UtcNow;

        var expiresAtUtc = now.AddMinutes(
            _jwtOptions.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.UserId.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now)
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
        };

        if (user.SessionId is { } sessionId)
            claims.Add(new Claim("sid", sessionId.ToString()));
        claims.Add(new Claim("amr", user.AuthenticationMethod));
        if (user.MfaVerified) claims.Add(new Claim("amr", "mfa"));

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(
                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Name,
                    user.UserName));
        }

        foreach (var role in user.Roles.Distinct())
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(
            accessToken,
            expiresAtUtc);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
