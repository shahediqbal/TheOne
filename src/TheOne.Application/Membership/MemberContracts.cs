using TheOne.Domain.Entities;

namespace TheOne.Application.Membership;

/// <summary>Staff request to record a payment against a member.</summary>
public sealed class RecordContributionRequest
{
    /// <summary>What the payment is for.</summary>
    public ContributionType Type { get; set; }
    /// <summary>Amount in BDT.</summary>
    public decimal Amount { get; set; }
    /// <summary>How it was paid.</summary>
    public ContributionMethod Method { get; set; }
    /// <summary>bKash transaction ID; required when <see cref="Method"/> is Bkash.</summary>
    public string? TransactionReference { get; set; }
    /// <summary>Free-text note, e.g. "January due" or "Founder's Day 2026".</summary>
    public string? Note { get; set; }
}

/// <summary>A single recorded contribution, as returned to staff.</summary>
public sealed record ContributionResponse(
    Guid Id, ContributionType Type, decimal Amount, ContributionMethod Method,
    string? TransactionReference, string? Note, DateTime RecordedAtUtc);

/// <summary>Staff request to reject an application, with a reason for the record.</summary>
public sealed class RejectMembershipApplicationRequest
{
    /// <summary>Why the application was rejected. Required — <see cref="MemberStatus.Rejected"/> should never be silent.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Current member state, as returned to staff.</summary>
public sealed record MemberResponse(
    Guid Id, Guid ApplicationId, string? MembershipNumber, MemberStatus Status,
    IReadOnlyCollection<ContributionResponse> Contributions);

/// <summary>Staff-facing use cases for reviewing and approving membership applications.</summary>
public interface IMembershipAdministrationService
{
    /// <summary>Marks a submitted application as staff-verified.</summary>
    Task<MemberResponse> VerifyAsync(string referenceCode, Guid staffId, CancellationToken ct);
    /// <summary>
    /// Approves a verified application. Refuses — via <see cref="MembershipFeeNotPaidException"/> —
    /// unless a <see cref="ContributionType.MembershipFee"/> contribution of at least 100 BDT has
    /// already been recorded for this member.
    /// </summary>
    Task<MemberResponse> ApproveAsync(string referenceCode, Guid staffId, CancellationToken ct);
    /// <summary>Rejects an application with a reason; terminal.</summary>
    Task<MemberResponse> RejectAsync(string referenceCode, Guid staffId, RejectMembershipApplicationRequest request, CancellationToken ct);
    /// <summary>Records a payment against a member. Available at any status; the fee gate is enforced only at approval.</summary>
    Task<ContributionResponse> RecordContributionAsync(string referenceCode, Guid staffId, RecordContributionRequest request, CancellationToken ct);
}

/// <summary>Persistence boundary for member records.</summary>
public interface IMemberStore
{
    /// <summary>Creates a member record for a newly submitted application.</summary>
    Task<Member> CreateAsync(Member member, CancellationToken ct);
    /// <summary>Finds the member record for a given application, or null when absent.</summary>
    Task<Member?> FindByApplicationIdAsync(Guid applicationId, CancellationToken ct);
    /// <summary>Persists changes to an already-loaded member.</summary>
    Task SaveAsync(Member member, CancellationToken ct);
    /// <summary>Generates the next unique membership number, assigned only on approval.</summary>
    Task<string> NextMembershipNumberAsync(CancellationToken ct);
}

/// <summary>Persistence boundary for contributions.</summary>
public interface IContributionStore
{
    /// <summary>Records a new contribution.</summary>
    Task<Contribution> CreateAsync(Contribution contribution, CancellationToken ct);
    /// <summary>Lists all contributions for a member, most recent first.</summary>
    Task<IReadOnlyCollection<Contribution>> ListByMemberAsync(Guid memberId, CancellationToken ct);
    /// <summary>Whether a qualifying membership fee (at least <paramref name="minimumAmount"/>) has been recorded.</summary>
    Task<bool> HasMembershipFeeAsync(Guid memberId, decimal minimumAmount, CancellationToken ct);
}

/// <summary>Signals that approval was attempted before the mandatory 100 BDT membership fee was recorded.</summary>
public sealed class MembershipFeeNotPaidException : Exception
{
    /// <summary>Creates the fee-gate error.</summary>
    public MembershipFeeNotPaidException()
        : base("The mandatory membership fee has not been recorded for this application yet.") { }
}

/// <summary>Signals that the referenced member record could not be found.</summary>
public sealed class MemberNotFoundException : Exception
{
    /// <summary>Creates the not-found error.</summary>
    public MemberNotFoundException() : base("No member record was found for this application.") { }
}
