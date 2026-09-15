using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using TheOne.Domain.Entities;

namespace TheOne.Application.Membership;

/// <summary>All membership mutations run in one transaction, serialized per application.</summary>
public interface IMembershipTransaction
{
    Task<T> ExecuteAsync<T>(string? referenceCode, Func<Task<T>> action, CancellationToken ct);
}

public sealed class MembershipConflictException(string message) : Exception(message);

public static class MembershipResumeCredential
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static bool Matches(string? token, string digest) =>
        token is { Length: 64 } && digest.Length == 64 &&
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(token)), Encoding.ASCII.GetBytes(digest));
}

/// <summary>The reference form's required fields are validated only at final submission.</summary>
public sealed class MembershipSubmissionPolicy
{
    public string[] RequiredFields { get; } = MembershipFormDefinition.RequiredFields;
    public MembershipSubmissionPolicy(string[]? requiredFields = null)
    {
        // Required reference fields cannot be disabled by a stale earlier-stage configuration.
        if (requiredFields is not null && requiredFields.Any(x => !MembershipFormDefinition.RequiredFields.Contains(x)))
            throw new InvalidOperationException("Unsupported membership required-field configuration.");
    }
    public void Validate(MembershipApplication application)
    {
        var failures = new List<ValidationFailure>();
        foreach (var (name, read) in MembershipFormDefinition.SubmissionFields)
        {
            var value = read(application);
            if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
                failures.Add(new(name, $"{name} is required before submission."));
        }
        if (application.HelpCategories == HelpCategory.None) failures.Add(new("HelpCategories", "Select an area of help."));
        if (application.TimeCommitment == MonthlyTimeCommitment.Unspecified) failures.Add(new("TimeCommitment", "Select a monthly time commitment."));
        if (application.ContributionIntent == FinancialContributionIntent.Unspecified) failures.Add(new("ContributionIntent", "Select a contribution intention."));
        if (application.HelpCategories.HasFlag(HelpCategory.Other) && string.IsNullOrWhiteSpace(application.OtherHelpBn)) failures.Add(new("OtherHelpBn", "Describe the other area of help."));
        if (application.PhotoUrl != "uploaded") failures.Add(new("PhotoUrl", "Upload an applicant photo."));
        // Revalidate stored values too: older drafts may predate today's shape/length rules.
        // Credentials were already checked by the submission service. This placeholder
        // only satisfies the save DTO validator and is never returned or persisted.
        // Translation provenance has its own stored representation; it is not form text.
        var stored = new SaveMembershipSectionRequest
        {
            ResumeToken = new string('0', 64),
            Email = application.Email,
            OtherHelpBn = application.OtherHelpBn,
            OtherHelpEn = application.OtherHelpEn,
            AboutSelfBn = application.AboutSelfBn,
            AboutSelfEn = application.AboutSelfEn,
            SignatureName = application.SignatureName,
            ApplicationDate = application.ApplicationDate,
            FullNameBn = application.FullNameBn,
            FullNameEn = application.FullNameEn,
            ContactNumber = application.ContactNumber ?? string.Empty,
            FatherNameBn = application.FatherNameBn,
            FatherNameEn = application.FatherNameEn,
            MotherNameBn = application.MotherNameBn,
            MotherNameEn = application.MotherNameEn,
            NidNumber = application.NidNumber,
            PermanentAddressBn = application.PermanentAddressBn,
            PermanentAddressEn = application.PermanentAddressEn,
            TemporaryAddressBn = application.TemporaryAddressBn,
            TemporaryAddressEn = application.TemporaryAddressEn,
            MaritalStatus = application.MaritalStatus,
            Age = application.Age,
            OccupationBn = application.OccupationBn,
            OccupationEn = application.OccupationEn,
            BloodGroup = application.BloodGroup,
            Gender = application.Gender,
            EducationBn = application.EducationBn,
            EducationEn = application.EducationEn,
            EmergencyContactNumber = application.EmergencyContactNumber,
            PhotoUrl = application.PhotoUrl,
            PurposeOfJoiningBn = application.PurposeOfJoiningBn,
            PurposeOfJoiningEn = application.PurposeOfJoiningEn,
            LifeGoalBn = application.LifeGoalBn,
            LifeGoalEn = application.LifeGoalEn,
            SpecialSkillsBn = application.SpecialSkillsBn,
            SpecialSkillsEn = application.SpecialSkillsEn,
            HelpCategories = application.HelpCategories,
            TimeCommitment = application.TimeCommitment,
            ContributionIntent = application.ContributionIntent,
            CurrentChallengeBn = application.CurrentChallengeBn,
            CurrentChallengeEn = application.CurrentChallengeEn,
            CommittedSinceYear = application.CommittedSinceYear,
        };
        failures.AddRange(new Validators.SaveMembershipSectionRequestValidator().Validate(stored).Errors);
        if (failures.Count > 0) throw new ValidationException(failures);
    }
}
