using FluentValidation;
using TheOne.Application.Authentication.DTOs;

namespace TheOne.Application.Authentication.Validators;

/// <summary>Validates RefreshTokenRequest input.</summary>
public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
    }
}