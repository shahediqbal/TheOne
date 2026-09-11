using FluentValidation;
using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates ResetPasswordRequest input.</summary>
public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches(@"^[0-9]{6}$");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword);
    }
}