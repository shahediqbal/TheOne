namespace TheOne.Application.Authentication.DTOs;

/// <summary>Identifies a challenge without exposing its code or account existence.</summary>
public sealed record OtpChallengeResponse(Guid ChallengeId, int ExpiresInSeconds = 300, int ResendAfterSeconds = 60);