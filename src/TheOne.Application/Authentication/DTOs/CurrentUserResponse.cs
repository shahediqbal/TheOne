namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains the signed-in user's safe profile fields and current role assignments.</summary>
public sealed record CurrentUserResponse(Guid UserId, string? FullName, string? Email,
    string? MobileNumber, bool EmailConfirmed, bool MobileConfirmed,
    DateTime CreatedAtUtc, IReadOnlyCollection<string> Roles);