using FluentValidation;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;
using Xunit;

namespace TheOne.IntegrationTests;

// These tests do not create an API host or connect to PostgreSQL.
public sealed class MembershipSubmissionPolicyTests
{
    private static MembershipApplication CompleteApplication() => new()
    {
        FullNameBn = "পরীক্ষা", FullNameEn = "Test Applicant", Email = "applicant@example.com",
        FatherNameBn = "পিতা", MotherNameBn = "মাতা", ContactNumber = "+8801700000000",
        NidNumber = "1234567890", PermanentAddressBn = "ঢাকা", TemporaryAddressBn = "ঢাকা",
        MaritalStatus = MaritalStatus.Unmarried, Age = 30, OccupationBn = "শিক্ষক",
        BloodGroup = "A+", Gender = Gender.Male, EducationBn = "স্নাতক",
        EmergencyContactNumber = "01700000001", PurposeOfJoiningBn = "সমাজসেবা",
        LifeGoalBn = "মানবসেবা", SpecialSkillsBn = "শিক্ষা", HelpCategories = HelpCategory.Education,
        TimeCommitment = MonthlyTimeCommitment.TwoHours,
        ContributionIntent = FinancialContributionIntent.DecideLater,
        CurrentChallengeBn = "প্রশিক্ষণ", CommittedSinceYear = 2020, AboutSelfBn = "শিখতে ভালোবাসি",
        SignatureName = "Test Applicant", ApplicationDate = new DateOnly(2026, 1, 1), PhotoUrl = "uploaded"
    };

    public static IEnumerable<object[]> RequiredFieldCases() =>
        MembershipFormDefinition.RequiredFields.Select(name => new object[] { name });

    // Reflection is intentionally confined to test discovery: newly added DTO text fields
    // must also be validated when loaded from storage, even if production mappings omit them.
    public static IEnumerable<object[]> StoredTextCases() =>
        typeof(SaveMembershipSectionRequest).GetProperties()
            .Where(property => property.PropertyType == typeof(string) &&
                property.Name != nameof(SaveMembershipSectionRequest.ResumeToken))
            .Select(property => new object[] { property.Name });

    [Fact]
    public void Complete_application_with_optional_translations_absent_is_valid()
    {
        new MembershipSubmissionPolicy().Validate(CompleteApplication());
    }

    [Theory]
    [MemberData(nameof(RequiredFieldCases))]
    public void Every_required_field_is_checked_at_submission(string name)
    {
        var application = CompleteApplication();
        var property = typeof(MembershipApplication).GetProperty(name);
        Assert.NotNull(property);
        property.SetValue(application, property.PropertyType.IsValueType
            ? Activator.CreateInstance(property.PropertyType) : null);
        var failure = Assert.Throws<ValidationException>(() => new MembershipSubmissionPolicy().Validate(application));
        Assert.Contains(failure.Errors, error => error.PropertyName == name);
    }

    [Theory]
    [MemberData(nameof(StoredTextCases))]
    public void Oversized_stored_text_cannot_bypass_save_validation(string name)
    {
        var application = CompleteApplication();
        var property = typeof(MembershipApplication).GetProperty(name);
        Assert.NotNull(property);
        property.SetValue(application, new string('x', 5001));
        var failure = Assert.Throws<ValidationException>(() => new MembershipSubmissionPolicy().Validate(application));
        Assert.Contains(failure.Errors, error => error.PropertyName == name);
    }

    [Fact]
    public void Stored_numeric_and_enum_values_are_revalidated()
    {
        var application = CompleteApplication();
        application.Age = -1;
        application.Gender = (Gender)999;
        application.CommittedSinceYear = 1800;
        var failure = Assert.Throws<ValidationException>(() => new MembershipSubmissionPolicy().Validate(application));
        Assert.Contains(failure.Errors, error => error.PropertyName == nameof(application.Age));
        Assert.Contains(failure.Errors, error => error.PropertyName == nameof(application.Gender));
        Assert.Contains(failure.Errors, error => error.PropertyName == nameof(application.CommittedSinceYear));
    }

    [Fact]
    public void Stale_configuration_cannot_disable_required_fields()
    {
        var application = CompleteApplication();
        application.Email = null;
        Assert.Throws<ValidationException>(() => new MembershipSubmissionPolicy([]).Validate(application));
        Assert.Throws<InvalidOperationException>(() => new MembershipSubmissionPolicy(["UnknownField"]));
    }
}
