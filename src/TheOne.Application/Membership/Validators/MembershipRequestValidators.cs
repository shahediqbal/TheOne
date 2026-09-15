using FluentValidation;
using TheOne.Domain.Entities;

namespace TheOne.Application.Membership.Validators;

public sealed class StartMembershipApplicationRequestValidator : AbstractValidator<StartMembershipApplicationRequest>
{
    public StartMembershipApplicationRequestValidator()
    {
        RuleFor(x => x.FullNameBn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullNameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactNumber).NotEmpty().Matches(@"^\+?[0-9]{7,15}$");
    }
}
public sealed class ResumeMembershipApplicationRequestValidator : AbstractValidator<ResumeMembershipApplicationRequest>
{
    public ResumeMembershipApplicationRequestValidator()
    {
        RuleFor(x => x.ReferenceCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.ContactNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ResumeToken).NotEmpty().Length(64);
    }
}
public sealed class SaveMembershipSectionRequestValidator : AbstractValidator<SaveMembershipSectionRequest>
{
    private static readonly string[] ProvenanceFields = ["FullName", "FatherName", "MotherName", "PermanentAddress",
        "TemporaryAddress", "Occupation", "Education", "PurposeOfJoining", "LifeGoal", "SpecialSkills", "CurrentChallenge", "OtherHelp", "AboutSelf"];
    public SaveMembershipSectionRequestValidator()
    {
        RuleFor(x => x.ContactNumber).NotEmpty().Matches(@"^\+?[0-9]{7,15}$");
        RuleFor(x => x.Email).MaximumLength(254).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.NidNumber).Matches(@"^[0-9]{1,30}$").When(x => !string.IsNullOrEmpty(x.NidNumber));
        RuleFor(x => x.EmergencyContactNumber).Matches(@"^\+?[0-9]{7,15}$").When(x => !string.IsNullOrEmpty(x.EmergencyContactNumber));
        RuleFor(x => x.BloodGroup).Must(x => new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" }.Contains(x)).When(x => !string.IsNullOrEmpty(x.BloodGroup));
        RuleFor(x => x.ApplicationDate).Must(x => x is null || x <= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6))).WithMessage("Application date cannot be in the future.");
        RuleFor(x => x.ResumeToken).NotEmpty().Length(64);
        RuleFor(x => x.Age).InclusiveBetween(0, 130).When(x => x.Age is not null);
        RuleFor(x => x.CommittedSinceYear).InclusiveBetween(1900, DateTime.UtcNow.Year).When(x => x.CommittedSinceYear is not null);
        RuleFor(x => x.Gender).IsInEnum().When(x => x.Gender is not null);
        RuleFor(x => x.MaritalStatus).IsInEnum().When(x => x.MaritalStatus is not null);
        RuleFor(x => x.TimeCommitment).IsInEnum().When(x => x.TimeCommitment is not null);
        RuleFor(x => x.ContributionIntent).IsInEnum().When(x => x.ContributionIntent is not null);
        RuleFor(x => x.HelpCategories).Must(x => x is null || ((int)x.Value & ~127) == 0);
        // Explicit accessors keep length constraints checked when DTO properties change.
        RuleFor(x => x.Email).MaximumLength(254);
        RuleFor(x => x.OtherHelpBn).MaximumLength(500);
        RuleFor(x => x.OtherHelpEn).MaximumLength(500);
        RuleFor(x => x.AboutSelfBn).MaximumLength(2000);
        RuleFor(x => x.AboutSelfEn).MaximumLength(2000);
        RuleFor(x => x.SignatureName).MaximumLength(200);
        RuleFor(x => x.ResumeToken).MaximumLength(64);
        RuleFor(x => x.FullNameBn).MaximumLength(200);
        RuleFor(x => x.FullNameEn).MaximumLength(200);
        RuleFor(x => x.ContactNumber).MaximumLength(20);
        RuleFor(x => x.FatherNameBn).MaximumLength(200);
        RuleFor(x => x.FatherNameEn).MaximumLength(200);
        RuleFor(x => x.MotherNameBn).MaximumLength(200);
        RuleFor(x => x.MotherNameEn).MaximumLength(200);
        RuleFor(x => x.NidNumber).MaximumLength(30);
        RuleFor(x => x.PermanentAddressBn).MaximumLength(1000);
        RuleFor(x => x.PermanentAddressEn).MaximumLength(1000);
        RuleFor(x => x.TemporaryAddressBn).MaximumLength(1000);
        RuleFor(x => x.TemporaryAddressEn).MaximumLength(1000);
        RuleFor(x => x.OccupationBn).MaximumLength(200);
        RuleFor(x => x.OccupationEn).MaximumLength(200);
        RuleFor(x => x.BloodGroup).MaximumLength(5);
        RuleFor(x => x.EducationBn).MaximumLength(200);
        RuleFor(x => x.EducationEn).MaximumLength(200);
        RuleFor(x => x.EmergencyContactNumber).MaximumLength(20);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.PurposeOfJoiningBn).MaximumLength(2000);
        RuleFor(x => x.PurposeOfJoiningEn).MaximumLength(2000);
        RuleFor(x => x.LifeGoalBn).MaximumLength(2000);
        RuleFor(x => x.LifeGoalEn).MaximumLength(2000);
        RuleFor(x => x.SpecialSkillsBn).MaximumLength(2000);
        RuleFor(x => x.SpecialSkillsEn).MaximumLength(2000);
        RuleFor(x => x.CurrentChallengeBn).MaximumLength(2000);
        RuleFor(x => x.CurrentChallengeEn).MaximumLength(2000);
        RuleFor(x => x).Custom((request, context) =>
        {
            if (request.FieldTranslationSources is { } sources)
                foreach (var (key, value) in sources)
                {
                    if (!ProvenanceFields.Contains(key) || !Enum.IsDefined(value))
                        context.AddFailure("FieldTranslationSources", "Unknown translation field or source.");
                    if (key is "FullName" or "FatherName" or "MotherName" &&
                        value is TranslationSource.MachineTranslated or TranslationSource.UserCorrected)
                        context.AddFailure("FieldTranslationSources", "Names must be entered manually.");
                }
        });
    }
}
public sealed class SubmitMembershipApplicationRequestValidator : AbstractValidator<SubmitMembershipApplicationRequest>
{
    public SubmitMembershipApplicationRequestValidator()
    {
        RuleFor(x => x.ContactNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ResumeToken).NotEmpty().Length(64);
        RuleFor(x => x.CodeOfConductAccepted).Equal(true);
        RuleFor(x => x.DeclarationAccepted).Equal(true);
        RuleFor(x => x.OathAccepted).Equal(true);
        RuleFor(x => x.ConsentVersion).Equal(MembershipFormDefinition.Version).WithMessage("Refresh the form and review the current consent text.");
    }
}
public sealed class RejectMembershipApplicationRequestValidator : AbstractValidator<RejectMembershipApplicationRequest>
{
    public RejectMembershipApplicationRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
}
public sealed class RecordContributionRequestValidator : AbstractValidator<RecordContributionRequest>
{
    public RecordContributionRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(99999999.99m)
            .Must(x => decimal.Round(x, 2) == x).WithMessage("Amount must have at most two decimal places.");
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.TransactionReference).NotEmpty().MaximumLength(100)
            .Must(x => x is not null && x.Trim().All(char.IsAsciiLetterOrDigit))
            .When(x => x.Method == ContributionMethod.Bkash);
        RuleFor(x => x.TransactionReference).Empty().When(x => x.Method == ContributionMethod.CashAtOffice);
    }
}
