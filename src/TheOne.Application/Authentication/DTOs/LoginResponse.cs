namespace TheOne.Application.Authentication.DTOs;

/// <summary>Returns tokens only after all required factors, otherwise a restricted challenge.</summary>
public sealed record LoginResponse(string? AccessToken, string? RefreshToken,
    DateTime? ExpiresAtUtc, DateTime? RefreshTokenExpiresAtUtc,
    bool RequiresTwoFactor, bool RequiresAuthenticatorSetup, Guid? ChallengeId)
{
    /// <summary>Creates a fully authenticated response.</summary>
    public static LoginResponse Complete(TokenResponse token) =>
        new(token.AccessToken, token.RefreshToken, token.ExpiresAtUtc, token.RefreshTokenExpiresAtUtc, false, false, null);
    /// <summary>Creates a response with no access or refresh credentials.</summary>
    public static LoginResponse Challenge(Guid id, bool enrollment) => new(null, null, null, null, !enrollment, enrollment, id);
}