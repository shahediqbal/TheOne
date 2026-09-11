using FluentValidation;
using TheOne.Application.Authentication.DTOs;
namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates SmsLoginRequest input.</summary>
public sealed class SmsLoginRequestValidator : AbstractValidator<SmsLoginRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public SmsLoginRequestValidator()
    {
        RuleFor(x => x.MobileNumber).NotEmpty().MaximumLength(20).Matches(@"^\+?[0-9]{7,15}$");
    }
}