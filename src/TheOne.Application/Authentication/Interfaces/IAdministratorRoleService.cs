using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Provides protected administrator promotion.</summary>
public interface IAdministratorRoleService
{
    /// <summary>Grants an administrator role only to an enrolled account and revokes its old sessions.</summary>
    Task AssignAsync(Guid actorId, Guid actorSessionId, Guid userId, AssignAdministratorRoleRequest request, CancellationToken cancellationToken);
}