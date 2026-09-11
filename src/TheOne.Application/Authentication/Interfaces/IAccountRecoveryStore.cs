using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Defines mobile verification and password-recovery operations.</summary>
public interface IAccountRecoveryStore
{
    /// <summary>Requests verification of the authenticated user's registered mobile.</summary>
    Task<OtpChallengeResponse> RequestMobileVerificationAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>Confirms the authenticated user's registered mobile.</summary>
    Task VerifyMobileAsync(Guid userId, VerifyMobileRequest request, CancellationToken cancellationToken = default);
    /// <summary>Requests recovery for an eligible account without disclosing account existence.</summary>
    Task<OtpChallengeResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    /// <summary>Resets a password using a valid recovery code and revokes refresh sessions.</summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    /// <summary>Requests an SMS login code for a verified, non-administrator account.</summary>
    Task<OtpChallengeResponse> RequestLoginOtpAsync(SmsLoginRequest request, CancellationToken cancellationToken = default);
    /// <summary>Consumes a login-only SMS code and creates a regular-user session.</summary>
    Task<TokenResponse> LoginWithOtpAsync(VerifyLoginOtpRequest request, CancellationToken cancellationToken = default);
}