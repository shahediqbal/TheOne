namespace TheOne.Application.Operations;

/// <summary>Explicit target and externally approved operation metadata; never contains credentials.</summary>
public sealed record OperatorRequest(Guid UserId, string ExpectedEmail, string Operator, string ApprovedBy, string Ticket, bool IdentityVerified);
/// <summary>Read-only eligibility information.</summary>
public sealed record OperatorInspection(Guid UserId, string Email, bool Active, bool ContactVerified, bool AuthenticatorEnabled,
    bool AuthenticatorReadable, bool LockedOut, bool Administrator, int SuperAdminCount);
/// <summary>Result of a completed bootstrap.</summary>
public sealed record BootstrapResult(Guid UserId, int RevokedSessions);
/// <summary>Short-lived enrollment capability returned only to the supervised operator terminal.</summary>
public sealed record OperatorRecoveryResult(Guid UserId, Guid EnrollmentChallengeId, int ExpiresInSeconds, int RevokedSessions);
/// <summary>Operator-only persistence boundary. Deliberately not registered by the public API.</summary>
public interface IOperatorAccountService
{
    /// <summary>Reads target state without changes.</summary>
    Task<OperatorInspection> InspectAsync(Guid userId, string expectedEmail, CancellationToken ct);
    /// <summary>Promotes the first enrolled account under an exclusive configuration lock.</summary>
    Task<BootstrapResult> BootstrapAsync(OperatorRequest request, CancellationToken ct);
    /// <summary>Recovers a named administrator; leaves password-only administrator login blocked.</summary>
    Task<OperatorRecoveryResult> RecoverAsync(OperatorRequest request, string newPassword, CancellationToken ct);
}
/// <summary>Safe operator-facing validation error.</summary>
public sealed class OperatorException(string message) : Exception(message);
