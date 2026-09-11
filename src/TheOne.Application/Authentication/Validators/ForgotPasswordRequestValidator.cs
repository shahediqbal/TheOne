using FluentValidation;
using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates ForgotPasswordRequest input.</summary>
public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.UserNameOrMobile).NotEmpty().MaximumLength(256);
    }
}