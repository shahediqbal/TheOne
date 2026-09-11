using FluentValidation;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Application.Authentication.Services;

/// <summary>Validates MFA requests before invoking Identity operations.</summary>
public sealed class MfaService(IMfaStore store, IValidator<PasswordConfirmationRequest> passwordValidator, IValidator<MfaChallengeRequest> challengeValidator,
    IValidator<MfaCodeRequest> codeValidator, IValidator<MfaRecoveryRequest> recoveryValidator, IValidator<AuthenticatorResetRequest> resetValidator) : IMfaService
{
    /// <inheritdoc />
    public async Task<LoginResponse> BeginEnrollmentAsync(Guid userId, PasswordConfirmationRequest request, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
        await passwordValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.BeginEnrollmentAsync(userId, request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<AuthenticatorSetupResponse> GetSetupAsync(MfaChallengeRequest request, CancellationToken cancellationToken)
    {
        await challengeValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.GetSetupAsync(request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<RecoveryCodesResponse> ConfirmSetupAsync(MfaCodeRequest request, CancellationToken cancellationToken)
    {
        await codeValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.ConfirmSetupAsync(request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<TokenResponse> CompleteLoginAsync(MfaCodeRequest request, CancellationToken cancellationToken)
    {
        await codeValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.CompleteLoginAsync(request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<TokenResponse> CompleteRecoveryLoginAsync(MfaRecoveryRequest request, CancellationToken cancellationToken)
    {
        await recoveryValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.CompleteRecoveryLoginAsync(request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<LoginResponse> ResetAsync(Guid userId, AuthenticatorResetRequest request, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
        await resetValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.ResetAsync(userId, request, cancellationToken);
    }
    /// <inheritdoc />
    public async Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(Guid userId, AuthenticatorResetRequest request, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
        await resetValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.RegenerateRecoveryCodesAsync(userId, request, cancellationToken);
    }
}