using FluentValidation;
using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates VerifyMobileRequest input.</summary>
public sealed class VerifyMobileRequestValidator : AbstractValidator<VerifyMobileRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public VerifyMobileRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches(@"^[0-9]{6}$");
    }
}