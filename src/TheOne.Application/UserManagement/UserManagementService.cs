using FluentValidation;
using TheOne.Application.Authentication;

namespace TheOne.Application.UserManagement;

/// <summary>Validates user-management requests before accessing persistence.</summary>
public sealed class UserManagementService(IUserManagementStore store, IValidator<UserListRequest> listValidator,
    IValidator<ChangeUserStatusRequest> statusValidator) : IUserManagementService
{
    /// <inheritdoc />
    public async Task<UserPageResponse> ListAsync(UserListRequest request, CancellationToken ct)
    {
        await listValidator.ValidateAndThrowAsync(request, ct);
        return await store.ListAsync(request, ct);
    }
    /// <inheritdoc />
    public Task<ManagedUserResponse?> GetAsync(Guid userId, CancellationToken ct) => store.GetAsync(userId, ct);
    /// <inheritdoc />
    public async Task<bool> ChangeStatusAsync(Guid actorId, Guid sessionId, Guid userId, ChangeUserStatusRequest request, CancellationToken ct)
    {
        if (actorId == Guid.Empty || sessionId == Guid.Empty) throw new AuthenticationException("Authentication is required.", true);
        await statusValidator.ValidateAndThrowAsync(request, ct);
        return await store.ChangeStatusAsync(actorId, sessionId, userId, request.IsActive!.Value, ct);
    }
}

/// <summary>Bounds database work and rejects invalid filters.</summary>
public sealed class UserListRequestValidator : AbstractValidator<UserListRequest>
{
    /// <summary>Creates validation rules.</summary>
    public UserListRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Role).MaximumLength(256);
    }
}

/// <summary>Prevents an omitted Boolean from accidentally deactivating an account.</summary>
public sealed class ChangeUserStatusRequestValidator : AbstractValidator<ChangeUserStatusRequest>
{
    /// <summary>Creates validation rules.</summary>
    public ChangeUserStatusRequestValidator() => RuleFor(x => x.IsActive).NotNull();
}
