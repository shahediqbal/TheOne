namespace TheOne.Domain.Entities;

/// <summary>Lifecycle of a membership application. No payment step: donations are a separate, unrelated flow.</summary>
public enum MembershipApplicationStatus
{
    /// <summary>Started but not yet finally submitted; editable via reference code.</summary>
    Draft = 0,
    /// <summary>Finally submitted by the applicant; awaiting staff verification.</summary>
    Submitted = 1,
    /// <summary>Staff has verified the submitted details.</summary>
    Verified = 2,
    /// <summary>An administrator has approved the application.</summary>
    Approved = 3,
    /// <summary>Membership is active.</summary>
    Active = 4,
    /// <summary>Application was rejected; terminal state.</summary>
    Rejected = 5
}

/// <summary>Provenance of a bilingual field's English value. Never applies to shared-value or enum-coded fields.</summary>
public enum TranslationSource
{
    /// <summary>No English value has been provided yet.</summary>
    None = 0,
    /// <summary>The applicant typed the English value directly; never auto-translated.</summary>
    UserTyped = 1,
    /// <summary>Suggested by the translation service and not yet reviewed by the applicant.</summary>
    MachineTranslated = 2,
    /// <summary>Machine-suggested, then edited by the applicant before submission.</summary>
    UserCorrected = 3
}

/// <summary>Applicant gender, as offered on the physical form.</summary>
public enum Gender
{
    /// <summary>Male.</summary>
    Male = 0,
    /// <summary>Female.</summary>
    Female = 1
}

/// <summary>Marital status, as offered on the physical form.</summary>
public enum MaritalStatus
{
    /// <summary>Unmarried.</summary>
    Unmarried = 0,
    /// <summary>Married.</summary>
    Married = 1,
    /// <summary>Divorced.</summary>
    Divorced = 2,
    /// <summary>Widowed.</summary>
    Widowed = 3,
    /// <summary>Separated.</summary>
    Separated = 4
}

/// <summary>Areas the applicant can help with. Flags: an applicant may select more than one.</summary>
[Flags]
public enum HelpCategory
{
    /// <summary>No category selected yet.</summary>
    None = 0,
    /// <summary>Education.</summary>
    Education = 1 << 0,
    /// <summary>Business.</summary>
    Business = 1 << 1,
    /// <summary>Technology.</summary>
    Technology = 1 << 2,
    /// <summary>Social work.</summary>
    SocialWork = 1 << 3,
    /// <summary>Medical.</summary>
    Medical = 1 << 4,
    /// <summary>Culture.</summary>
    Culture = 1 << 5,
    /// <summary>Other, unlisted category.</summary>
    Other = 1 << 6
}

/// <summary>Monthly time the applicant can commit, as offered on the physical form.</summary>
public enum MonthlyTimeCommitment
{
    /// <summary>Not yet specified.</summary>
    Unspecified = 0,
    /// <summary>Two hours per month.</summary>
    TwoHours = 1,
    /// <summary>Five hours per month.</summary>
    FiveHours = 2,
    /// <summary>Ten hours per month.</summary>
    TenHours = 3
}

/// <summary>Whether the applicant intends to contribute financially.</summary>
public enum FinancialContributionIntent
{
    /// <summary>Not yet specified.</summary>
    Unspecified = 0,
    /// <summary>Yes.</summary>
    Yes = 1,
    /// <summary>No.</summary>
    No = 2,
    /// <summary>Will decide later.</summary>
    DecideLater = 3
}

/// <summary>
/// A membership registration application, submitted publicly and anonymously.
/// Deliberately has no required link to an Identity account: <see cref="UserId"/> stays null
/// until an explicit, audited linking step is introduced alongside membership login.
/// Contains no payment fields; Payments are recorded separately against the associated member.
/// </summary>
public sealed class MembershipApplication
{
    public string? Email { get; set; }
    public string? OtherHelpBn { get; set; }
    public string? OtherHelpEn { get; set; }
    public string? AboutSelfBn { get; set; }
    public string? AboutSelfEn { get; set; }
    public string? SignatureName { get; set; }
    public DateOnly? ApplicationDate { get; set; }
    public bool DeclarationAccepted { get; set; }
    public bool OathAccepted { get; set; }
    public DateTime? ConsentAcceptedAtUtc { get; set; }
    public string? ConsentVersion { get; set; }

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable support reference (not an access credential), e.g. "SDR-2026-000123". Assigned once, at step 1.</summary>
    public string ReferenceCode { get; set; } = string.Empty;
    /// <summary>SHA-256 digest of the private resume credential. Never returned to clients.</summary>
    public string ResumeTokenHash { get; set; } = string.Empty;
    public Guid? VerifiedBy { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public MembershipApplicationStatus Status { get; set; } = MembershipApplicationStatus.Draft;

    /// <summary>Optional link to an Identity account, populated only by a later, explicit linking operation.</summary>
    public Guid? UserId { get; set; }

    // ----- Basic Information -----

    /// <summary>Full name in Bangla, as the applicant typed it.</summary>
    public string? FullNameBn { get; set; }
    /// <summary>
    /// Full name in English. Transliterated, not translated, and never auto-suggested:
    /// the applicant must type this against their identity document.
    /// </summary>
    public string? FullNameEn { get; set; }

    /// <summary>Father's name in Bangla.</summary>
    public string? FatherNameBn { get; set; }
    /// <summary>Father's name in English. Same transliteration-only rule as <see cref="FullNameEn"/>.</summary>
    public string? FatherNameEn { get; set; }

    /// <summary>Mother's name in Bangla.</summary>
    public string? MotherNameBn { get; set; }
    /// <summary>Mother's name in English. Same transliteration-only rule as <see cref="FullNameEn"/>.</summary>
    public string? MotherNameEn { get; set; }

    /// <summary>Contact phone number. Single shared value; no translation applies.</summary>
    public string? ContactNumber { get; set; }

    /// <summary>National ID number. Single shared value; no translation applies.</summary>
    public string? NidNumber { get; set; }

    /// <summary>Permanent address in Bangla.</summary>
    public string? PermanentAddressBn { get; set; }
    /// <summary>Permanent address in English; may be machine-translated, always reviewable.</summary>
    public string? PermanentAddressEn { get; set; }

    /// <summary>Temporary address in Bangla.</summary>
    public string? TemporaryAddressBn { get; set; }
    /// <summary>Temporary address in English; may be machine-translated, always reviewable.</summary>
    public string? TemporaryAddressEn { get; set; }

    /// <summary>Marital status.</summary>
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>Age in years. Shared value; no translation applies.</summary>
    public int? Age { get; set; }

    /// <summary>Occupation in Bangla.</summary>
    public string? OccupationBn { get; set; }
    /// <summary>Occupation in English; may be machine-translated.</summary>
    public string? OccupationEn { get; set; }

    /// <summary>Blood group, e.g. "A+". Shared value; no translation applies.</summary>
    public string? BloodGroup { get; set; }

    /// <summary>Gender.</summary>
    public Gender? Gender { get; set; }

    /// <summary>Educational qualification in Bangla.</summary>
    public string? EducationBn { get; set; }
    /// <summary>Educational qualification in English; may be machine-translated.</summary>
    public string? EducationEn { get; set; }

    /// <summary>Emergency contact phone number. Shared value; no translation applies.</summary>
    public string? EmergencyContactNumber { get; set; }

    /// <summary>Path/URL to the uploaded passport-size photo.</summary>
    public string? PhotoUrl { get; set; }

    // ----- Purpose & Mindset -----

    /// <summary>Reason for joining, in Bangla.</summary>
    public string? PurposeOfJoiningBn { get; set; }
    /// <summary>Reason for joining, in English; low-stakes free text, eligible for self-hosted translation.</summary>
    public string? PurposeOfJoiningEn { get; set; }

    /// <summary>Life goal, in Bangla.</summary>
    public string? LifeGoalBn { get; set; }
    /// <summary>Life goal, in English; low-stakes free text, eligible for self-hosted translation.</summary>
    public string? LifeGoalEn { get; set; }

    // ----- Skills & Contribution -----

    /// <summary>Special skills, in Bangla.</summary>
    public string? SpecialSkillsBn { get; set; }
    /// <summary>Special skills, in English; low-stakes free text, eligible for self-hosted translation.</summary>
    public string? SpecialSkillsEn { get; set; }

    /// <summary>Areas the applicant can help with; bitwise-combined.</summary>
    public HelpCategory HelpCategories { get; set; } = HelpCategory.None;

    /// <summary>Monthly time commitment.</summary>
    public MonthlyTimeCommitment TimeCommitment { get; set; } = MonthlyTimeCommitment.Unspecified;

    /// <summary>Financial contribution intent.</summary>
    public FinancialContributionIntent ContributionIntent { get; set; } = FinancialContributionIntent.Unspecified;

    // ----- Current Challenge & Plans -----

    /// <summary>Current life challenge and expected help, in Bangla.</summary>
    public string? CurrentChallengeBn { get; set; }
    /// <summary>Current life challenge and expected help, in English; low-stakes free text.</summary>
    public string? CurrentChallengeEn { get; set; }

    // ----- Code of Conduct -----

    /// <summary>Whether the applicant accepted the code of conduct. Required to submit.</summary>
    public bool CodeOfConductAccepted { get; set; }

    /// <summary>Year the applicant became committed to the society's ideals, if disclosed.</summary>
    public int? CommittedSinceYear { get; set; }

    /// <summary>Date of formal membership acceptance, set by staff at approval.</summary>
    public DateOnly? FormalMembershipDate { get; set; }

    // ----- Provenance -----

    /// <summary>
    /// Allowlisted JSON map of bilingual field name to <see cref="TranslationSource"/>, e.g.
    /// {"purposeOfJoining":"MachineTranslated","lifeGoal":"UserCorrected"}. Mirrors the audit
    /// convention used elsewhere: structured, non-secret metadata rather than free-form notes.
    /// </summary>
    public string TranslationProvenance { get; set; } = "{}";

    // ----- Lifecycle timestamps -----

    /// <summary>When the draft was first created.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When any field was last saved.</summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>When the applicant finally submitted (Draft to Submitted transition).</summary>
    public DateTime? SubmittedAtUtc { get; set; }

    /// <summary>When staff verified the submission.</summary>
    public DateTime? VerifiedAtUtc { get; set; }

    /// <summary>When an administrator approved the application.</summary>
    public DateTime? ApprovedAtUtc { get; set; }
    /// <summary>Administrator who approved the application.</summary>
    public Guid? ApprovedBy { get; set; }
}
