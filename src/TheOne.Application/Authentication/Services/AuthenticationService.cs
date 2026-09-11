using FluentValidation;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Application.Authentication.Services;

/// <summary>Validates authentication requests before invoking the Identity adapter.</summary>
public sealed class AuthenticationService(
    IAuthenticationStore store,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshValidator,
    IValidator<ChangePasswordRequest> passwordValidator) : IAuthenticationService
{
    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.RegisterAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.LoginAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        await refreshValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await store.RefreshAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        await refreshValidator.ValidateAndThrowAsync(request, cancellationToken);
        await store.LogoutAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
        await passwordValidator.ValidateAndThrowAsync(request, cancellationToken);
        await store.ChangePasswordAsync(userId, request, cancellationToken);
    }
}