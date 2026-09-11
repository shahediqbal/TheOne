namespace TheOne.Application.Abstractions.Authentication;

/// <summary>
/// Represents the minimum authenticated user information required
/// for generating security tokens.
/// </summary>
public sealed record TokenUser(
    Guid UserId,
    string? Email,
    string? UserName,
    IReadOnlyCollection<string> Roles,
    Guid? SessionId = null,
    bool MfaVerified = false,
    string AuthenticationMethod = "pwd");
