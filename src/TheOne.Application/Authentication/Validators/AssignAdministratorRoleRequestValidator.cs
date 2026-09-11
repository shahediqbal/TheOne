using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates AssignAdministratorRoleRequest input.</summary>
public sealed class AssignAdministratorRoleRequestValidator : AbstractValidator<AssignAdministratorRoleRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public AssignAdministratorRoleRequestValidator()
    {
        RuleFor(x => x.Role).Must(x => x == LoginSecurityPolicy.Admin || x == LoginSecurityPolicy.SuperAdmin).WithMessage("Role must be Admin or SuperAdmin.");
    }
}