using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Provides password-proven enrollment and MFA completion operations.</summary>
public interface IMfaStore
{
    /// <summary>Starts enrollment after fresh password verification.</summary>
    Task<LoginResponse> BeginEnrollmentAsync(Guid userId, PasswordConfirmationRequest request, CancellationToken cancellationToken);
    /// <summary>Returns a key and QR code only for a restricted enrollment challenge.</summary>
    Task<AuthenticatorSetupResponse> GetSetupAsync(MfaChallengeRequest request, CancellationToken cancellationToken);
    /// <summary>Confirms possession of the authenticator and returns single-use recovery codes.</summary>
    Task<RecoveryCodesResponse> ConfirmSetupAsync(MfaCodeRequest request, CancellationToken cancellationToken);
    /// <summary>Completes a password-proven login with an authenticator code.</summary>
    Task<TokenResponse> CompleteLoginAsync(MfaCodeRequest request, CancellationToken cancellationToken);
    /// <summary>Completes a password-proven login with a single-use recovery code.</summary>
    Task<TokenResponse> CompleteRecoveryLoginAsync(MfaRecoveryRequest request, CancellationToken cancellationToken);
    /// <summary>Requires fresh password and second-factor proof before replacing the authenticator.</summary>
    Task<LoginResponse> ResetAsync(Guid userId, AuthenticatorResetRequest request, CancellationToken cancellationToken);
    /// <summary>Requires fresh credentials before replacing recovery codes.</summary>
    Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(Guid userId, AuthenticatorResetRequest request, CancellationToken cancellationToken);
}