using TheOne.Application.Membership;
using TheOne.Domain.Entities;
namespace TheOne.IntegrationTests;
internal static class MembershipFormFixture
{
    internal const string Photo = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=";
    internal static SaveMembershipSectionRequest Fields(string token = "") => new()
    {
        ResumeToken=token, FullNameBn="পরীক্ষা", FullNameEn="Test Applicant", ContactNumber="+8801700000000",
        Email="applicant@example.com", FatherNameBn="পিতা", MotherNameBn="মাতা", NidNumber="1234567890",
        PermanentAddressBn="ঢাকা", TemporaryAddressBn="ঢাকা", MaritalStatus=MaritalStatus.Unmarried, Age=30,
        OccupationBn="শিক্ষক", BloodGroup="A+", Gender=Gender.Male, EducationBn="স্নাতক", EmergencyContactNumber="01700000001",
        PurposeOfJoiningBn="সমাজসেবা", LifeGoalBn="মানবসেবা", SpecialSkillsBn="শিক্ষা", HelpCategories=HelpCategory.Education,
        TimeCommitment=MonthlyTimeCommitment.TwoHours, ContributionIntent=FinancialContributionIntent.DecideLater,
        CurrentChallengeBn="প্রশিক্ষণ", CommittedSinceYear=2020, AboutSelfBn="শিখতে ভালোবাসি", SignatureName="Test Applicant",
        ApplicationDate=new DateOnly(2026,1,1)
    };
}
