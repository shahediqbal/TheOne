using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Provides Identity and session persistence operations.</summary>
public interface IAuthenticationStore
{
    /// <summary>Registers an account and assigns the default role.</summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    /// <summary>Authenticates credentials and creates a session.</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    /// <summary>Rotates an active refresh token atomically.</summary>
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    /// <summary>Revokes a refresh session.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    /// <summary>Changes the password and revokes all refresh sessions.</summary>
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}