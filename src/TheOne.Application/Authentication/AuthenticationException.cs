namespace TheOne.Application.Authentication;

/// <summary>Represents an expected failure with safe client-facing details.</summary>
public sealed class AuthenticationException(string message, bool invalidCredentials = false,
    IReadOnlyCollection<string>? errors = null) : Exception(message)
{
    /// <summary>Indicates failed authentication rather than invalid input.</summary>
    public bool InvalidCredentials { get; } = invalidCredentials;
    /// <summary>Gets safe validation details.</summary>
    public IReadOnlyCollection<string> Errors { get; } = errors ?? [];
}