namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Sends transactional SMS without exposing provider details to the application.</summary>
public interface ISmsSender
{
    /// <summary>Checks whether delivery has been configured; never sends a message.</summary>
    void EnsureAvailable();
    /// <summary>Submits a message once; errors must not include message text or credentials.</summary>
    Task SendAsync(string mobileNumber, string message, CancellationToken cancellationToken);
}