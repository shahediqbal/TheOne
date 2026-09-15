using FluentValidation;
using TheOne.Application.Membership.Validators;
using TheOne.Domain.Entities;

namespace TheOne.Application.Membership;

/// <summary>Staff review workflow for membership applications. The 100 BDT fee gate lives here, not in the public flow.</summary>
public sealed class MembershipAdministrationService(
    IMembershipApplicationStore applications,
    IMemberStore members,
    IContributionStore contributions, IMembershipTransaction transaction) : IMembershipAdministrationService
{
    public Task<MemberResponse> VerifyAsync(string referenceCode, Guid staffId, CancellationToken ct) =>
        transaction.ExecuteAsync(referenceCode, () => VerifyCoreAsync(referenceCode, staffId, ct), ct);
    public Task<MemberResponse> ApproveAsync(string referenceCode, Guid staffId, CancellationToken ct) =>
        transaction.ExecuteAsync(referenceCode, () => ApproveCoreAsync(referenceCode, staffId, ct), ct);
    public Task<MemberResponse> RejectAsync(string referenceCode, Guid staffId, RejectMembershipApplicationRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(referenceCode, () => RejectCoreAsync(referenceCode, staffId, request, ct), ct);
    public Task<ContributionResponse> RecordContributionAsync(string referenceCode, Guid staffId, RecordContributionRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(referenceCode, () => RecordContributionCoreAsync(referenceCode, staffId, request, ct), ct);

    /// <summary>The mandatory one-time membership fee, in BDT. A single source of truth for the gate.</summary>
    public const decimal MembershipFeeAmount = 100m;

    /// <inheritdoc />
    private async Task<MemberResponse> VerifyCoreAsync(string referenceCode, Guid staffId, CancellationToken ct)
    {
        var (application, member) = await LoadAsync(referenceCode, ct);
        if (application.Status == MembershipApplicationStatus.Verified) return await ToResponseAsync(member, ct);
        RequireState(application, MembershipApplicationStatus.Submitted);
        if (application.Status == MembershipApplicationStatus.Submitted)
        {
            application.Status = MembershipApplicationStatus.Verified;
            application.VerifiedAtUtc = DateTime.UtcNow;
            application.VerifiedBy = staffId;
            application.ModifiedAtUtc = application.VerifiedAtUtc;
            await applications.SaveAsync(application, ct);
        }
        return await ToResponseAsync(member, ct);
    }

    /// <inheritdoc />
    private async Task<MemberResponse> ApproveCoreAsync(string referenceCode, Guid staffId, CancellationToken ct)
    {
        var (application, member) = await LoadAsync(referenceCode, ct);

        if (application.Status == MembershipApplicationStatus.Approved) return await ToResponseAsync(member, ct);
        RequireState(application, MembershipApplicationStatus.Verified);
        var feePaid = await contributions.HasMembershipFeeAsync(member.Id, MembershipFeeAmount, ct);
        if (!feePaid)
        {
            throw new MembershipFeeNotPaidException();
        }

        application.Status = MembershipApplicationStatus.Approved;
        application.ApprovedAtUtc = DateTime.UtcNow;
        application.ApprovedBy = staffId;
        application.ModifiedAtUtc = application.ApprovedAtUtc;
        application.FormalMembershipDate = DateOnly.FromDateTime(application.ApprovedAtUtc.Value);

        member.MembershipNumber = await members.NextMembershipNumberAsync(ct);
        member.Status = MemberStatus.Active;
        member.ActivatedAtUtc = application.ApprovedAtUtc;

        await applications.SaveAsync(application, ct);
        await members.SaveAsync(member, ct);
        return await ToResponseAsync(member, ct);
    }

    /// <inheritdoc />
    private async Task<MemberResponse> RejectCoreAsync(string referenceCode, Guid staffId, RejectMembershipApplicationRequest request, CancellationToken ct)
    {
        await new RejectMembershipApplicationRequestValidator().ValidateAndThrowAsync(request, ct);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("A reason is required to reject an application.", nameof(request));
        }

        var (application, member) = await LoadAsync(referenceCode, ct);
        if (application.Status == MembershipApplicationStatus.Rejected) return await ToResponseAsync(member, ct);
        RequireState(application, MembershipApplicationStatus.Submitted, MembershipApplicationStatus.Verified);
        application.RejectionReason = request.Reason.Trim();
        application.RejectedBy = staffId;
        application.RejectedAtUtc = DateTime.UtcNow;
        application.Status = MembershipApplicationStatus.Rejected;
        application.ModifiedAtUtc = DateTime.UtcNow;
        member.Status = MemberStatus.Rejected;

        await applications.SaveAsync(application, ct);
        await members.SaveAsync(member, ct);
        return await ToResponseAsync(member, ct);
    }

    /// <inheritdoc />
    private async Task<ContributionResponse> RecordContributionCoreAsync(string referenceCode, Guid staffId, RecordContributionRequest request, CancellationToken ct)
    {
        await new RecordContributionRequestValidator().ValidateAndThrowAsync(request, ct);
        var (_, member) = await LoadAsync(referenceCode, ct);

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be positive.", nameof(request));
        }
        if (request.Method == ContributionMethod.Bkash && string.IsNullOrWhiteSpace(request.TransactionReference))
        {
            throw new ArgumentException("A bKash transaction reference is required for bKash payments.", nameof(request));
        }

        var contribution = new Contribution
        {
            MemberId = member.Id,
            Type = request.Type,
            Amount = request.Amount,
            Method = request.Method,
            TransactionReference = request.Method == ContributionMethod.Bkash ? request.TransactionReference!.Trim().ToUpperInvariant() : null,
            Note = request.Note,
            RecordedByStaffId = staffId
        };
        var created = await contributions.CreateAsync(contribution, ct);
        return new ContributionResponse(created.Id, created.Type, created.Amount, created.Method,
            created.TransactionReference, created.Note, created.RecordedAtUtc);
    }

    private static void RequireState(MembershipApplication application, params MembershipApplicationStatus[] allowed)
    {
        if (!allowed.Contains(application.Status))
            throw new MembershipConflictException("This action is not allowed for the current application status.");
    }

    private async Task<(MembershipApplication Application, Member Member)> LoadAsync(string referenceCode, CancellationToken ct)
    {
        var application = await applications.FindByReferenceCodeAsync(referenceCode, ct)
            ?? throw new MembershipApplicationNotFoundException();
        var member = await members.FindByApplicationIdAsync(application.Id, ct)
            ?? throw new MemberNotFoundException();
        return (application, member);
    }

    private async Task<MemberResponse> ToResponseAsync(Member member, CancellationToken ct)
    {
        var history = await contributions.ListByMemberAsync(member.Id, ct);
        var responses = history
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => new ContributionResponse(x.Id, x.Type, x.Amount, x.Method, x.TransactionReference, x.Note, x.RecordedAtUtc))
            .ToList();
        return new MemberResponse(member.Id, member.ApplicationId, member.MembershipNumber, member.Status, responses);
    }
}
