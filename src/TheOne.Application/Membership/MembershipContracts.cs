using TheOne.Domain.Entities;

namespace TheOne.Application.Membership;

/// <summary>Step 1 payload: creates a draft and issues a reference code. Public, anonymous.</summary>
public sealed class StartMembershipApplicationRequest
{
    /// <summary>Full name in Bangla.</summary>
    public string FullNameBn { get; set; } = string.Empty;
    /// <summary>Full name in English, typed by the applicant against their document. Never auto-suggested.</summary>
    public string FullNameEn { get; set; } = string.Empty;
    /// <summary>Contact phone number; also the resume-match value.</summary>
    public string ContactNumber { get; set; } = string.Empty;
}

/// <summary>
/// Partial save of one form section for an existing draft. All fields optional: only supplied
/// fields are updated, matching the multi-step "save and continue" flow. Section is identified
/// implicitly by which fields are populated.
/// </summary>
public sealed class SaveMembershipSectionRequest
{
    public string? Email { get; set; }
    public string? OtherHelpBn { get; set; }
    public string? OtherHelpEn { get; set; }
    public string? AboutSelfBn { get; set; }
    public string? AboutSelfEn { get; set; }
    public string? SignatureName { get; set; }
    public DateOnly? ApplicationDate { get; set; }
    public string ResumeToken { get; set; } = string.Empty;
    public string? FullNameBn { get; set; }
    public string? FullNameEn { get; set; }
    /// <summary>Contact number, required on every save as the additional contact match; the private resume token is also required.</summary>
    public string ContactNumber { get; set; } = string.Empty;

    /// <summary>Father's name in Bangla.</summary>
    public string? FatherNameBn { get; set; }
    /// <summary>Father's name in English, typed by the applicant. Never auto-suggested.</summary>
    public string? FatherNameEn { get; set; }
    /// <summary>Mother's name in Bangla.</summary>
    public string? MotherNameBn { get; set; }
    /// <summary>Mother's name in English, typed by the applicant. Never auto-suggested.</summary>
    public string? MotherNameEn { get; set; }
    /// <summary>National ID number.</summary>
    public string? NidNumber { get; set; }
    /// <summary>Permanent address in Bangla.</summary>
    public string? PermanentAddressBn { get; set; }
    /// <summary>Permanent address in English; may arrive as a reviewed machine suggestion.</summary>
    public string? PermanentAddressEn { get; set; }
    /// <summary>Temporary address in Bangla.</summary>
    public string? TemporaryAddressBn { get; set; }
    /// <summary>Temporary address in English; may arrive as a reviewed machine suggestion.</summary>
    public string? TemporaryAddressEn { get; set; }
    /// <summary>Marital status.</summary>
    public MaritalStatus? MaritalStatus { get; set; }
    /// <summary>Age in years.</summary>
    public int? Age { get; set; }
    /// <summary>Occupation in Bangla.</summary>
    public string? OccupationBn { get; set; }
    /// <summary>Occupation in English.</summary>
    public string? OccupationEn { get; set; }
    /// <summary>Blood group, e.g. "A+".</summary>
    public string? BloodGroup { get; set; }
    /// <summary>Gender.</summary>
    public Gender? Gender { get; set; }
    /// <summary>Educational qualification in Bangla.</summary>
    public string? EducationBn { get; set; }
    /// <summary>Educational qualification in English.</summary>
    public string? EducationEn { get; set; }
    /// <summary>Emergency contact phone number.</summary>
    public string? EmergencyContactNumber { get; set; }
    /// <summary>Path/URL to the uploaded passport-size photo.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Reason for joining, in Bangla.</summary>
    public string? PurposeOfJoiningBn { get; set; }
    /// <summary>Reason for joining, in English.</summary>
    public string? PurposeOfJoiningEn { get; set; }
    /// <summary>Life goal, in Bangla.</summary>
    public string? LifeGoalBn { get; set; }
    /// <summary>Life goal, in English.</summary>
    public string? LifeGoalEn { get; set; }

    /// <summary>Special skills, in Bangla.</summary>
    public string? SpecialSkillsBn { get; set; }
    /// <summary>Special skills, in English.</summary>
    public string? SpecialSkillsEn { get; set; }
    /// <summary>Areas the applicant can help with.</summary>
    public HelpCategory? HelpCategories { get; set; }
    /// <summary>Monthly time commitment.</summary>
    public MonthlyTimeCommitment? TimeCommitment { get; set; }
    /// <summary>Financial contribution intent.</summary>
    public FinancialContributionIntent? ContributionIntent { get; set; }

    /// <summary>Current life challenge and expected help, in Bangla.</summary>
    public string? CurrentChallengeBn { get; set; }
    /// <summary>Current life challenge and expected help, in English.</summary>
    public string? CurrentChallengeEn { get; set; }

    /// <summary>Year the applicant became committed to the society's ideals.</summary>
    public int? CommittedSinceYear { get; set; }

    /// <summary>
    /// Per-field translation source for any bilingual field included in this save, keyed by the
    /// property name (e.g. "PurposeOfJoining"). Missing entries default to <see cref="TranslationSource.None"/>.
    /// </summary>
    public Dictionary<string, TranslationSource>? FieldTranslationSources { get; set; }
}

/// <summary>Final submission: locks the draft and moves it into the verification workflow.</summary>
public sealed class SubmitMembershipApplicationRequest
{
    public bool DeclarationAccepted { get; set; }
    public bool OathAccepted { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public string ResumeToken { get; set; } = string.Empty;
    /// <summary>Contact number, as the additional contact match; the private resume token is also required.</summary>
    public string ContactNumber { get; set; } = string.Empty;
    /// <summary>Must be true; submission is refused otherwise.</summary>
    public bool CodeOfConductAccepted { get; set; }
}

/// <summary>Resumes an existing draft by reference code, private resume token and contact number match. No OTP: membership is deliberately plain.</summary>
public sealed class ResumeMembershipApplicationRequest
{
    public string ResumeToken { get; set; } = string.Empty;
    /// <summary>Human-readable reference issued at step 1; not a secret.</summary>
    public string ReferenceCode { get; set; } = string.Empty;
    /// <summary>Contact number supplied at step 1, matched exactly.</summary>
    public string ContactNumber { get; set; } = string.Empty;
}

/// <summary>Full current state of an application, returned on resume and after each save.</summary>
public sealed record MembershipApplicationResponse(
    string ReferenceCode,
    MembershipApplicationStatus Status,
    string? FullNameBn, string? FullNameEn,
    string? FatherNameBn, string? FatherNameEn,
    string? MotherNameBn, string? MotherNameEn,
    string? ContactNumber, string? NidNumber,
    string? PermanentAddressBn, string? PermanentAddressEn,
    string? TemporaryAddressBn, string? TemporaryAddressEn,
    MaritalStatus? MaritalStatus, int? Age,
    string? OccupationBn, string? OccupationEn,
    string? BloodGroup, Gender? Gender,
    string? EducationBn, string? EducationEn,
    string? EmergencyContactNumber, string? PhotoUrl,
    string? PurposeOfJoiningBn, string? PurposeOfJoiningEn,
    string? LifeGoalBn, string? LifeGoalEn,
    string? SpecialSkillsBn, string? SpecialSkillsEn,
    HelpCategory HelpCategories, MonthlyTimeCommitment TimeCommitment,
    FinancialContributionIntent ContributionIntent,
    string? CurrentChallengeBn, string? CurrentChallengeEn,
    bool CodeOfConductAccepted, int? CommittedSinceYear,
    DateTime CreatedAtUtc, DateTime? ModifiedAtUtc, DateTime? SubmittedAtUtc,
    string? Email, string? OtherHelpBn, string? OtherHelpEn, string? AboutSelfBn, string? AboutSelfEn,
    string? SignatureName, DateOnly? ApplicationDate, bool DeclarationAccepted, bool OathAccepted,
    DateTime? ConsentAcceptedAtUtc, string? ConsentVersion);

/// <summary>Result of starting an application: only the reference code and initial state are needed by the client.</summary>
public sealed record StartMembershipApplicationResponse(string ReferenceCode, MembershipApplicationStatus Status, string ResumeToken);

/// <summary>Validated, publicly exposed membership-application use cases.</summary>
public interface IMembershipApplicationService
{
    /// <summary>Creates a new draft and issues a reference code, returned once to the applicant; SMS delivery is not implemented.</summary>
    Task<StartMembershipApplicationResponse> StartAsync(StartMembershipApplicationRequest request, CancellationToken ct);
    /// <summary>Resumes a draft; the private token, reference code and contact number must match.</summary>
    Task<MembershipApplicationResponse> ResumeAsync(ResumeMembershipApplicationRequest request, CancellationToken ct);
    /// <summary>Saves any subset of section fields to an existing draft.</summary>
    Task<MembershipApplicationResponse> SaveSectionAsync(string referenceCode, SaveMembershipSectionRequest request, CancellationToken ct);
    /// <summary>Finally submits a draft, moving it to <see cref="MembershipApplicationStatus.Submitted"/>.</summary>
    Task<MembershipApplicationResponse> SubmitAsync(string referenceCode, SubmitMembershipApplicationRequest request, CancellationToken ct);
}

/// <summary>Persistence boundary for membership applications.</summary>
public interface IMembershipApplicationStore
{
    /// <summary>Inserts a new draft with a freshly issued reference code.</summary>
    Task<MembershipApplication> CreateDraftAsync(MembershipApplication application, CancellationToken ct);
    /// <summary>Finds an application by reference code, or null when absent.</summary>
    Task<MembershipApplication?> FindByReferenceCodeAsync(string referenceCode, CancellationToken ct);
    /// <summary>Persists changes to an already-loaded application.</summary>
    Task SaveAsync(MembershipApplication application, CancellationToken ct);
    /// <summary>Generates the next unique, human-readable reference code for the given year.</summary>
    Task<string> NextReferenceCodeAsync(int year, CancellationToken ct);
}

/// <summary>Signals that a draft could not be found, or the contact number did not match.</summary>
public sealed class MembershipApplicationNotFoundException : Exception
{
    /// <summary>Creates an error that never reveals whether the reference code or the contact number was wrong.</summary>
    public MembershipApplicationNotFoundException() : base("No matching application was found.") { }
}

/// <summary>Signals an operation attempted on an application that is no longer a draft.</summary>
public sealed class MembershipApplicationNotEditableException : Exception
{
    /// <summary>Creates an error for edits attempted after final submission.</summary>
    public MembershipApplicationNotEditableException() : base("This application has already been submitted and can no longer be edited.") { }
}
