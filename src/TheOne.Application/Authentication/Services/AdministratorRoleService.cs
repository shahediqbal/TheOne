using FluentValidation;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
namespace TheOne.Application.Authentication.Services;

/// <summary>Validates administrator promotion requests.</summary>
public sealed class AdministratorRoleService(IAdministratorRoleStore store,
    IValidator<AssignAdministratorRoleRequest> validator) : IAdministratorRoleService
{
    /// <inheritdoc />
    public async Task AssignAsync(Guid actorId, Guid actorSessionId, Guid userId, AssignAdministratorRoleRequest request, CancellationToken ct)
    {
        if (actorId == Guid.Empty || actorSessionId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
        if (userId == Guid.Empty) throw new AuthenticationException("A user identifier is required.");
        await validator.ValidateAndThrowAsync(request, ct);
        await store.AssignAsync(actorId, actorSessionId, userId, request, ct);
    }
}