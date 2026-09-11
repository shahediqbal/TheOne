namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains AssignAdministratorRole input.</summary>
public sealed class AssignAdministratorRoleRequest
{
    /// <summary>Gets or sets Role.</summary>
    public string Role { get; set; } = string.Empty;
}