using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates MfaChallengeRequest input.</summary>
public sealed class MfaChallengeRequestValidator : AbstractValidator<MfaChallengeRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public MfaChallengeRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
    }
}