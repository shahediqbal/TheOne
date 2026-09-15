namespace TheOne.Domain.Entities;

/// <summary>Lifecycle of a member record, distinct from the application that created it.</summary>
public enum MemberStatus
{
    /// <summary>Application submitted; a membership fee may or may not have been recorded yet.</summary>
    PendingApproval = 0,
    /// <summary>Approved and active.</summary>
    Active = 1,
    /// <summary>Previously active, currently not.</summary>
    Inactive = 2,
    /// <summary>The associated application was rejected; terminal state.</summary>
    Rejected = 3
}

/// <summary>What a recorded payment is for. Distinct types because only one of them gates approval.</summary>
public enum ContributionType
{
    /// <summary>The mandatory 100 BDT one-time membership fee. The only type that gates approval.</summary>
    MembershipFee = 0,
    /// <summary>Regular dues. Amount and accounting period remain undecided; no arrears calculation.</summary>
    MonthlyDue = 1,
    /// <summary>A voluntary contribution tied to a specific occasion (e.g. Founder's Day, 22 Sep).</summary>
    EventContribution = 2,
    /// <summary>A general, untied donation.</summary>
    Donation = 3
}

/// <summary>How a contribution was paid.</summary>
public enum ContributionMethod
{
    /// <summary>Paid via bKash; reconciled manually, verified manually by staff.</summary>
    Bkash = 0,
    /// <summary>Paid in person at the office.</summary>
    CashAtOffice = 1
}

/// <summary>
/// A member record, created when a <see cref="MembershipApplication"/> is submitted so that
/// contributions have something to attach to before approval is decided. Independent of Identity:
/// <see cref="LinkedUserId"/> stays null until an explicit, audited linking step, same as
/// <see cref="MembershipApplication.UserId"/>.
/// </summary>
public sealed class Member
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The application this member record was created from.</summary>
    public Guid ApplicationId { get; set; }

    /// <summary>How this member's application was originally entered.</summary>
    public MembershipEntryChannel EntryChannel { get; set; }

    /// <summary>Staff identifier, when entered by an operator on the applicant's behalf.</summary>
    public Guid? EnteredByStaffId { get; set; }

    /// <summary>Assigned only on approval; never reused if an application is rejected.</summary>
    public string? MembershipNumber { get; set; }

    /// <summary>Current status.</summary>
    public MemberStatus Status { get; set; } = MemberStatus.PendingApproval;

    /// <summary>Optional link to an Identity account, populated only by a later, explicit linking operation.</summary>
    public Guid? LinkedUserId { get; set; }

    /// <summary>When this record was created (at application submission).</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When the member became active.</summary>
    public DateTime? ActivatedAtUtc { get; set; }
}

/// <summary>How a membership application was entered.</summary>
public enum MembershipEntryChannel
{
    /// <summary>The applicant filled the public form themselves.</summary>
    Self = 0,
    /// <summary>A staff operator entered it on the applicant's behalf, from a physical paper form.</summary>
    Operator = 1
}

/// <summary>
/// A recorded payment against a member. Recording is always manual — even bKash payments are
/// logged by staff with a transaction reference, never auto-confirmed via webhook, matching the
/// project's existing bKash-donation convention.
/// </summary>
public sealed class Contribution
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The member this contribution belongs to.</summary>
    public Guid MemberId { get; set; }

    /// <summary>What the payment is for.</summary>
    public ContributionType Type { get; set; }

    /// <summary>Amount in BDT.</summary>
    public decimal Amount { get; set; }

    /// <summary>How it was paid.</summary>
    public ContributionMethod Method { get; set; }

    /// <summary>bKash transaction ID, when applicable. Null for cash-at-office.</summary>
    public string? TransactionReference { get; set; }

    /// <summary>Free-text note, e.g. "January due" or "Founder's Day 2026".</summary>
    public string? Note { get; set; }

    /// <summary>Staff member who recorded this entry.</summary>
    public Guid RecordedByStaffId { get; set; }

    /// <summary>When this entry was recorded.</summary>
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
