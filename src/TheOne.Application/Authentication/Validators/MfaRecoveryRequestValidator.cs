using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates MfaRecoveryRequest input.</summary>
public sealed class MfaRecoveryRequestValidator : AbstractValidator<MfaRecoveryRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public MfaRecoveryRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.RecoveryCode).NotEmpty().MaximumLength(100);
    }
}