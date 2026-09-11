using FluentValidation;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Application.Authentication.Services;

/// <summary>Validates recovery requests before calling the persistence adapter.</summary>
public sealed class AccountRecoveryService(IAccountRecoveryStore store,
    IValidator<ForgotPasswordRequest> forgotValidator,
    IValidator<VerifyMobileRequest> verifyValidator,
    IValidator<ResetPasswordRequest> resetValidator,
    IValidator<SmsLoginRequest> smsLoginValidator,
    IValidator<VerifyLoginOtpRequest> loginCodeValidator) : IAccountRecoveryService
{
    /// <inheritdoc />
    public Task<OtpChallengeResponse> RequestMobileVerificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        return store.RequestMobileVerificationAsync(userId, cancellationToken);
    }
    /// <inheritdoc />
    public async Task VerifyMobileAsync(Guid userId, VerifyMobileRequest request, CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        await verifyValidator.ValidateAndThrowAsync(request, cancellationToken);
        await store.VerifyMobileAsync(userId, request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<OtpChallengeResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await forgotValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.ForgotPasswordAsync(request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await resetValidator.ValidateAndThrowAsync(request, cancellationToken);
        await store.ResetPasswordAsync(request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<OtpChallengeResponse> RequestLoginOtpAsync(SmsLoginRequest request, CancellationToken cancellationToken = default)
    {
        await smsLoginValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.RequestLoginOtpAsync(request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<TokenResponse> LoginWithOtpAsync(VerifyLoginOtpRequest request, CancellationToken cancellationToken = default)
    {
        await loginCodeValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.LoginWithOtpAsync(request, cancellationToken);
    }
    private static void RequireUser(Guid userId)
    {
        if (userId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
    }
}
