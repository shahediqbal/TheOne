using FluentValidation;
namespace TheOne.Application.Administration;

/// <summary>Validates inputs independently of persistence.</summary>
public sealed class AdministrationService(IAdministrationStore store) : IAdministrationService
{
    /// <inheritdoc />
    public Task<IReadOnlyCollection<RoleResponse>> RolesAsync(CancellationToken ct) => store.RolesAsync(ct);
    /// <inheritdoc />
    public async Task<RoleResponse> SaveRoleAsync(Guid actor, Guid session, Guid? id, RoleRequest request, CancellationToken ct)
    {
        await new RoleValidator().ValidateAndThrowAsync(request, ct);
        return await store.SaveRoleAsync(actor, session, id, request, ct);
    }
    /// <inheritdoc />
    public Task DeleteRoleAsync(Guid actor, Guid session, Guid id, CancellationToken ct) => store.DeleteRoleAsync(actor, session, id, ct);
    /// <inheritdoc />
    public Task AssignRoleAsync(Guid actor, Guid session, Guid userId, Guid roleId, bool assign, CancellationToken ct) => store.AssignRoleAsync(actor, session, userId, roleId, assign, ct);
    /// <inheritdoc />
    public Task<IReadOnlyCollection<MenuResponse>> MenusAsync(CancellationToken ct) => store.MenusAsync(ct);
    /// <inheritdoc />
    public async Task<MenuResponse> SaveMenuAsync(Guid actor, Guid session, Guid? id, MenuRequest request, CancellationToken ct)
    {
        await new MenuValidator().ValidateAndThrowAsync(request, ct);
        return await store.SaveMenuAsync(actor, session, id, request, ct);
    }
    /// <inheritdoc />
    public Task DeleteMenuAsync(Guid actor, Guid session, Guid id, CancellationToken ct) => store.DeleteMenuAsync(actor, session, id, ct);
    /// <inheritdoc />
    public Task<IReadOnlyCollection<NavigationNode>> NavigationAsync(Guid userId, bool mfa, CancellationToken ct) => store.NavigationAsync(userId, mfa, ct);
    /// <inheritdoc />
    public async Task<AuditPage> AuditAsync(AuditQuery request, CancellationToken ct)
    {
        await new AuditValidator().ValidateAndThrowAsync(request, ct);
        return await store.AuditAsync(request, ct);
    }
}
/// <summary>Role input validation.</summary>
public sealed class RoleValidator : AbstractValidator<RoleRequest>
{
    /// <summary>Creates rules.</summary>
    public RoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).Matches("^[A-Za-z][A-Za-z0-9 _-]*$");
        RuleFor(x => x.Permissions).NotNull().Must(x => x is not null && x.Length <= Permissions.All.Count && x.Distinct().Count() == x.Length && x.All(Permissions.All.Contains));
    }
}
/// <summary>Menu input validation.</summary>
public sealed class MenuValidator : AbstractValidator<MenuRequest>
{
    /// <summary>Creates rules.</summary>
    public MenuValidator()
    {
        RuleFor(x => x.LabelEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LabelBn).MaximumLength(100);
        RuleFor(x => x.Icon).MaximumLength(60).Matches("^[a-zA-Z0-9_-]*$").When(x => x.Icon is not null);
        RuleFor(x => x.Route).MaximumLength(200).Matches("^/[a-zA-Z0-9/_-]*$").Must(x => x is null || !x.Contains("//")).When(x => x.Route is not null);
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 10000);
        RuleFor(x => x.RequiredPermission).Must(x => x is null || Permissions.All.Contains(x));
        RuleFor(x => x.RoleIds).NotNull().Must(x => x is not null && x.Length <= 100 && x.All(id => id != Guid.Empty) && x.Distinct().Count() == x.Length);
    }
}
/// <summary>Audit query validation.</summary>
public sealed class AuditValidator : AbstractValidator<AuditQuery>
{
    /// <summary>Creates rules.</summary>
    public AuditValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Action).MaximumLength(100);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithMessage("From must precede To.");
    }
}
