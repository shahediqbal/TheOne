using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates PasswordConfirmationRequest input.</summary>
public sealed class PasswordConfirmationRequestValidator : AbstractValidator<PasswordConfirmationRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public PasswordConfirmationRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}