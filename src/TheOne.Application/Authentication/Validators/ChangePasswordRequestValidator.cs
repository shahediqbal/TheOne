using FluentValidation;
using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates ChangePasswordRequest input.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128).NotEqual(x => x.CurrentPassword);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword);
    }
}