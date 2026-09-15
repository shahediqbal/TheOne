using TheOne.Domain.Entities;

namespace TheOne.Application.Membership;
public static class MembershipFormDefinition
{
    public const string Version = "sadria-membership-2026-09-v1";
    // Each accessor and its name refer to the same entity property at compile time.
    internal static readonly (string Name, Func<MembershipApplication, object?> Read)[] SubmissionFields =
    [
        (nameof(MembershipApplication.FullNameBn), application => application.FullNameBn),
        (nameof(MembershipApplication.FullNameEn), application => application.FullNameEn),
        (nameof(MembershipApplication.Email), application => application.Email),
        (nameof(MembershipApplication.FatherNameBn), application => application.FatherNameBn),
        (nameof(MembershipApplication.MotherNameBn), application => application.MotherNameBn),
        (nameof(MembershipApplication.ContactNumber), application => application.ContactNumber),
        (nameof(MembershipApplication.NidNumber), application => application.NidNumber),
        (nameof(MembershipApplication.PermanentAddressBn), application => application.PermanentAddressBn),
        (nameof(MembershipApplication.TemporaryAddressBn), application => application.TemporaryAddressBn),
        (nameof(MembershipApplication.MaritalStatus), application => application.MaritalStatus),
        (nameof(MembershipApplication.Age), application => application.Age),
        (nameof(MembershipApplication.OccupationBn), application => application.OccupationBn),
        (nameof(MembershipApplication.BloodGroup), application => application.BloodGroup),
        (nameof(MembershipApplication.Gender), application => application.Gender),
        (nameof(MembershipApplication.EducationBn), application => application.EducationBn),
        (nameof(MembershipApplication.EmergencyContactNumber), application => application.EmergencyContactNumber),
        (nameof(MembershipApplication.PurposeOfJoiningBn), application => application.PurposeOfJoiningBn),
        (nameof(MembershipApplication.LifeGoalBn), application => application.LifeGoalBn),
        (nameof(MembershipApplication.SpecialSkillsBn), application => application.SpecialSkillsBn),
        (nameof(MembershipApplication.HelpCategories), application => application.HelpCategories),
        (nameof(MembershipApplication.TimeCommitment), application => application.TimeCommitment),
        (nameof(MembershipApplication.ContributionIntent), application => application.ContributionIntent),
        (nameof(MembershipApplication.CurrentChallengeBn), application => application.CurrentChallengeBn),
        (nameof(MembershipApplication.CommittedSinceYear), application => application.CommittedSinceYear),
        (nameof(MembershipApplication.AboutSelfBn), application => application.AboutSelfBn),
        (nameof(MembershipApplication.SignatureName), application => application.SignatureName),
        (nameof(MembershipApplication.ApplicationDate), application => application.ApplicationDate),
        (nameof(MembershipApplication.PhotoUrl), application => application.PhotoUrl),
    ];

    public static readonly string[] RequiredFields = SubmissionFields.Select(field => field.Name).ToArray();
    public const string ConductBn = "১. আমি সোসাইটির সকল নিয়ম-শৃঙ্খলা মেনে চলব।\n২. সাদরিয়া সমাজের পারস্পরিক ভ্রাতৃত্ব এবং ঐক্য বজায় রাখব।";
    public const string DeclarationBn = "আমি ঘোষনা করছি যে এই ফরমে প্রদত্ত সকল তথ্য সত্য এবং আমি সোসাইটির কল্যাণে সচেষ্ট থাকব।";
    public const string OathBn = "আমি শপথ করছি যে, সাদরিয়া সোসাইটির আদর্শ, বিধিবিধান ও আচরণবিধি মেনে সততা, আত্মশুদ্ধি, ভ্রাতৃত্ব, মানবসেবা ও ঐক্যের চেতনায় একজন আদর্শ সদস্য হিসেবে নিজেকে গড়ে তুলবো এবং সোসাইটির মর্যাদা ও কল্যাণে সর্বদা আন্তরিকভাবে কাজ করবো।";
    public const string ConductEn = "1. I will follow the Society's rules and discipline.\n2. I will uphold mutual fellowship and unity in the Sadria community.";
    public const string DeclarationEn = "I declare that the information in this form is true and that I will work for the welfare of the Society.";
    public const string OathEn = "I pledge to follow the ideals, rules and code of conduct of Sadria Society; to develop myself through honesty, self-purification, fellowship, service to humanity and unity; and to work sincerely for the Society's dignity and welfare.";
}
