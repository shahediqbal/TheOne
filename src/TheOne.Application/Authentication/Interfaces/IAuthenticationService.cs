using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Defines validated authentication use cases.</summary>
public interface IAuthenticationService
{
    /// <summary>Registers an account and assigns the default role.</summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    /// <summary>Authenticates credentials and creates a session.</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    /// <summary>Rotates an active refresh token atomically.</summary>
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    /// <summary>Revokes a refresh session.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    /// <summary>Changes the password and revokes all refresh sessions.</summary>
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}