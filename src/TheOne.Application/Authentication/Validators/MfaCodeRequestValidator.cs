using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates MfaCodeRequest input.</summary>
public sealed class MfaCodeRequestValidator : AbstractValidator<MfaCodeRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public MfaCodeRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches(@"^[0-9]{6}$");
    }
}