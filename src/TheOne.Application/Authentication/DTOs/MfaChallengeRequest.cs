namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains MfaChallenge input.</summary>
public sealed class MfaChallengeRequest
{
    /// <summary>Gets or sets ChallengeId.</summary>
    public Guid ChallengeId { get; set; }
}