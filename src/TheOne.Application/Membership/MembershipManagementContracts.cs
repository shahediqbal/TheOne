using TheOne.Domain.Entities;
namespace TheOne.Application.Membership;

public sealed record MembershipQuery(int Page = 1, int PageSize = 20, string? Search = null, MembershipApplicationStatus? Status = null);
public sealed record MembershipListItem(Guid Id, string ReferenceCode, string? FullNameBn, string? FullNameEn,
    string? ContactNumber, MembershipApplicationStatus Status, DateTime CreatedAtUtc, string? MembershipNumber);
public sealed record MembershipPage(IReadOnlyCollection<MembershipListItem> Items, int TotalCount, int Page, int PageSize);
public sealed record MembershipDetail(MembershipApplicationResponse Application, MemberResponse? Member,
    Guid? VerifiedBy, DateTime? VerifiedAtUtc, Guid? ApprovedBy, DateTime? ApprovedAtUtc,
    Guid? RejectedBy, DateTime? RejectedAtUtc, string? RejectionReason,
    MembershipEntryChannel? EntryChannel, Guid? EnteredByStaffId);
public sealed class OperatorMembershipRequest
{
    public Guid RequestId { get; set; }
    public bool DeclarationAccepted { get; set; }
    public bool OathAccepted { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public string PhotoBase64 { get; set; } = string.Empty;
    public SaveMembershipSectionRequest Fields { get; set; } = new();
    public bool CodeOfConductAccepted { get; set; }
}
public interface IMembershipManagement
{
    Task<MembershipPage> ListAsync(MembershipQuery query, CancellationToken ct);
    Task<MembershipDetail> DetailAsync(string referenceCode, CancellationToken ct);
    Task<MembershipDetail> EnterAsync(Guid staffId, OperatorMembershipRequest request, CancellationToken ct);
}
