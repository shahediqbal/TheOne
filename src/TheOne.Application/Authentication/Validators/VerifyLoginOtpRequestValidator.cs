using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates VerifyLoginOtpRequest input.</summary>
public sealed class VerifyLoginOtpRequestValidator : AbstractValidator<VerifyLoginOtpRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public VerifyLoginOtpRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches(@"^[0-9]{6}$");
    }
}