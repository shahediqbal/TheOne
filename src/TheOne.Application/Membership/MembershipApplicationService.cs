using FluentValidation;
using TheOne.Application.Membership.Validators;
using System.Text.Json;
using TheOne.Domain.Entities;

namespace TheOne.Application.Membership;

/// <summary>Validated use cases for the public membership registration flow. No OTP, no payment: deliberately plain.</summary>
public sealed class MembershipApplicationService(IMembershipApplicationStore store, IMemberStore members, IMembershipTransaction transaction, MembershipSubmissionPolicy submissionPolicy) : IMembershipApplicationService
{
    public Task<StartMembershipApplicationResponse> StartAsync(StartMembershipApplicationRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(null, () => StartCoreAsync(request, ct), ct);
    public Task<MembershipApplicationResponse> SaveSectionAsync(string referenceCode, SaveMembershipSectionRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(referenceCode, () => SaveSectionCoreAsync(referenceCode, request, ct), ct);
    public Task<MembershipApplicationResponse> SubmitAsync(string referenceCode, SubmitMembershipApplicationRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(referenceCode, () => SubmitCoreAsync(referenceCode, request, ct), ct);

    /// <inheritdoc />
    private async Task<StartMembershipApplicationResponse> StartCoreAsync(StartMembershipApplicationRequest request, CancellationToken ct)
    {
        await new StartMembershipApplicationRequestValidator().ValidateAndThrowAsync(request, ct);
        var token = MembershipResumeCredential.Create();
        var referenceCode = await store.NextReferenceCodeAsync(DateTime.UtcNow.Year, ct);
        var application = new MembershipApplication
        {
            ReferenceCode = referenceCode,
            ResumeTokenHash = MembershipResumeCredential.Hash(token),
            Status = MembershipApplicationStatus.Draft,
            FullNameBn = request.FullNameBn,
            FullNameEn = request.FullNameEn,
            ContactNumber = request.ContactNumber
        };
        var created = await store.CreateDraftAsync(application, ct);
        return new StartMembershipApplicationResponse(created.ReferenceCode, created.Status, token);
    }

    /// <inheritdoc />
    public async Task<MembershipApplicationResponse> ResumeAsync(ResumeMembershipApplicationRequest request, CancellationToken ct)
    {
        await new ResumeMembershipApplicationRequestValidator().ValidateAndThrowAsync(request, ct);
        var application = await store.FindByReferenceCodeAsync(request.ReferenceCode, ct)
            ?? throw new MembershipApplicationNotFoundException();
        if (!MembershipResumeCredential.Matches(request.ResumeToken, application.ResumeTokenHash) || !string.Equals(application.ContactNumber, request.ContactNumber, StringComparison.Ordinal))
        {
            // Same exception as "not found": never reveal whether the code or the number was wrong.
            throw new MembershipApplicationNotFoundException();
        }
        return ToResponse(application);
    }

    /// <inheritdoc />
    private async Task<MembershipApplicationResponse> SaveSectionCoreAsync(string referenceCode, SaveMembershipSectionRequest request, CancellationToken ct)
    {
        var application = await LoadEditableAsync(referenceCode, request.ContactNumber, request.ResumeToken, ct);

        await new SaveMembershipSectionRequestValidator().ValidateAndThrowAsync(request, ct);
        if (request.Email is not null) application.Email = request.Email;
        if (request.OtherHelpBn is not null) application.OtherHelpBn = request.OtherHelpBn;
        if (request.OtherHelpEn is not null) application.OtherHelpEn = request.OtherHelpEn;
        if (request.AboutSelfBn is not null) application.AboutSelfBn = request.AboutSelfBn;
        if (request.AboutSelfEn is not null) application.AboutSelfEn = request.AboutSelfEn;
        if (request.SignatureName is not null) application.SignatureName = request.SignatureName;
        if (request.ApplicationDate is not null) application.ApplicationDate = request.ApplicationDate;
        if (request.FullNameBn is not null) application.FullNameBn = request.FullNameBn;
        if (request.FullNameEn is not null) application.FullNameEn = request.FullNameEn;
        if (request.FatherNameBn is not null) application.FatherNameBn = request.FatherNameBn;
        if (request.FatherNameEn is not null) application.FatherNameEn = request.FatherNameEn;
        if (request.MotherNameBn is not null) application.MotherNameBn = request.MotherNameBn;
        if (request.MotherNameEn is not null) application.MotherNameEn = request.MotherNameEn;
        if (request.NidNumber is not null) application.NidNumber = request.NidNumber;
        if (request.PermanentAddressBn is not null) application.PermanentAddressBn = request.PermanentAddressBn;
        if (request.PermanentAddressEn is not null) application.PermanentAddressEn = request.PermanentAddressEn;
        if (request.TemporaryAddressBn is not null) application.TemporaryAddressBn = request.TemporaryAddressBn;
        if (request.TemporaryAddressEn is not null) application.TemporaryAddressEn = request.TemporaryAddressEn;
        if (request.MaritalStatus is not null) application.MaritalStatus = request.MaritalStatus;
        if (request.Age is not null) application.Age = request.Age;
        if (request.OccupationBn is not null) application.OccupationBn = request.OccupationBn;
        if (request.OccupationEn is not null) application.OccupationEn = request.OccupationEn;
        if (request.BloodGroup is not null) application.BloodGroup = request.BloodGroup;
        if (request.Gender is not null) application.Gender = request.Gender;
        if (request.EducationBn is not null) application.EducationBn = request.EducationBn;
        if (request.EducationEn is not null) application.EducationEn = request.EducationEn;
        if (request.EmergencyContactNumber is not null) application.EmergencyContactNumber = request.EmergencyContactNumber;
        // PhotoUrl is server-owned; only the protected upload endpoint can set it.
        if (request.PurposeOfJoiningBn is not null) application.PurposeOfJoiningBn = request.PurposeOfJoiningBn;
        if (request.PurposeOfJoiningEn is not null) application.PurposeOfJoiningEn = request.PurposeOfJoiningEn;
        if (request.LifeGoalBn is not null) application.LifeGoalBn = request.LifeGoalBn;
        if (request.LifeGoalEn is not null) application.LifeGoalEn = request.LifeGoalEn;
        if (request.SpecialSkillsBn is not null) application.SpecialSkillsBn = request.SpecialSkillsBn;
        if (request.SpecialSkillsEn is not null) application.SpecialSkillsEn = request.SpecialSkillsEn;
        if (request.HelpCategories is not null) application.HelpCategories = request.HelpCategories.Value;
        if (request.TimeCommitment is not null) application.TimeCommitment = request.TimeCommitment.Value;
        if (request.ContributionIntent is not null) application.ContributionIntent = request.ContributionIntent.Value;
        if (request.CurrentChallengeBn is not null) application.CurrentChallengeBn = request.CurrentChallengeBn;
        if (request.CurrentChallengeEn is not null) application.CurrentChallengeEn = request.CurrentChallengeEn;
        if (request.CommittedSinceYear is not null) application.CommittedSinceYear = request.CommittedSinceYear;

        if (request.FieldTranslationSources is { Count: > 0 })
        {
            application.TranslationProvenance = MergeProvenance(application.TranslationProvenance, request.FieldTranslationSources);
        }

        application.ModifiedAtUtc = DateTime.UtcNow;
        await store.SaveAsync(application, ct);
        return ToResponse(application);
    }

    /// <inheritdoc />
    private async Task<MembershipApplicationResponse> SubmitCoreAsync(string referenceCode, SubmitMembershipApplicationRequest request, CancellationToken ct)
    {
        var application = await LoadEditableAsync(referenceCode, request.ContactNumber, request.ResumeToken, ct);
        await new SubmitMembershipApplicationRequestValidator().ValidateAndThrowAsync(request, ct);
        submissionPolicy.Validate(application);
        if (!request.CodeOfConductAccepted)
        {
            throw new ArgumentException("The code of conduct must be accepted to submit.", nameof(request));
        }

        application.CodeOfConductAccepted = true;
        application.DeclarationAccepted = request.DeclarationAccepted;
        application.OathAccepted = request.OathAccepted;
        application.ConsentVersion = MembershipFormDefinition.Version;
        application.ConsentAcceptedAtUtc = DateTime.UtcNow;
        application.Status = MembershipApplicationStatus.Submitted;
        application.SubmittedAtUtc = DateTime.UtcNow;
        application.ModifiedAtUtc = application.SubmittedAtUtc;
        await store.SaveAsync(application, ct);

        // A Member record is created here, not at approval, so a contribution (the mandatory
        // membership fee) has something to attach to before the approval decision is made.
        await members.CreateAsync(new Member
        {
            ApplicationId = application.Id,
            EntryChannel = MembershipEntryChannel.Self,
            Status = MemberStatus.PendingApproval
        }, ct);

        return ToResponse(application);
    }

    private async Task<MembershipApplication> LoadEditableAsync(string referenceCode, string contactNumber, string resumeToken, CancellationToken ct)
    {
        var application = await store.FindByReferenceCodeAsync(referenceCode, ct)
            ?? throw new MembershipApplicationNotFoundException();
        if (!MembershipResumeCredential.Matches(resumeToken, application.ResumeTokenHash) || !string.Equals(application.ContactNumber, contactNumber, StringComparison.Ordinal))
        {
            throw new MembershipApplicationNotFoundException();
        }
        if (application.Status != MembershipApplicationStatus.Draft)
        {
            throw new MembershipApplicationNotEditableException();
        }
        return application;
    }

    private static string MergeProvenance(string existingJson, Dictionary<string, TranslationSource> updates)
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, TranslationSource>>(existingJson)
            ?? new Dictionary<string, TranslationSource>();
        foreach (var (field, source) in updates)
        {
            map[field] = source;
        }
        return JsonSerializer.Serialize(map);
    }

    public static MembershipApplicationResponse ToResponse(MembershipApplication a) => new(
        a.ReferenceCode, a.Status,
        a.FullNameBn, a.FullNameEn,
        a.FatherNameBn, a.FatherNameEn,
        a.MotherNameBn, a.MotherNameEn,
        a.ContactNumber, a.NidNumber,
        a.PermanentAddressBn, a.PermanentAddressEn,
        a.TemporaryAddressBn, a.TemporaryAddressEn,
        a.MaritalStatus, a.Age,
        a.OccupationBn, a.OccupationEn,
        a.BloodGroup, a.Gender,
        a.EducationBn, a.EducationEn,
        a.EmergencyContactNumber, a.PhotoUrl,
        a.PurposeOfJoiningBn, a.PurposeOfJoiningEn,
        a.LifeGoalBn, a.LifeGoalEn,
        a.SpecialSkillsBn, a.SpecialSkillsEn,
        a.HelpCategories, a.TimeCommitment, a.ContributionIntent,
        a.CurrentChallengeBn, a.CurrentChallengeEn,
        a.CodeOfConductAccepted, a.CommittedSinceYear,
        a.CreatedAtUtc, a.ModifiedAtUtc, a.SubmittedAtUtc,
        a.Email, a.OtherHelpBn, a.OtherHelpEn, a.AboutSelfBn, a.AboutSelfEn, a.SignatureName,
        a.ApplicationDate, a.DeclarationAccepted, a.OathAccepted, a.ConsentAcceptedAtUtc, a.ConsentVersion);
}
