namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Rechecks MFA policy against current account roles and session state.</summary>
public interface IPrivilegedSessionValidator
{
    /// <summary>Rejects elevated or MFA sessions which no longer meet current server policy.</summary>
    Task<bool> ValidateAsync(Guid userId, Guid? sessionId, bool mfaClaim,
        IReadOnlyCollection<string> claimedRoles, CancellationToken cancellationToken);
}