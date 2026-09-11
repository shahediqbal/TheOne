using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates AuthenticatorResetRequest input.</summary>
public sealed class AuthenticatorResetRequestValidator : AbstractValidator<AuthenticatorResetRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public AuthenticatorResetRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x).Must(x => string.IsNullOrEmpty(x.Code) != string.IsNullOrEmpty(x.RecoveryCode)).WithMessage("Provide either an authenticator code or a recovery code.");
        RuleFor(x => x.Code).Matches(@"^[0-9]{6}$").When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.RecoveryCode).MaximumLength(100);
    }
}