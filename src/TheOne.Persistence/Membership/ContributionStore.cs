using Microsoft.EntityFrameworkCore;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;

namespace TheOne.Persistence.Membership;

/// <summary>EF Core-backed persistence for contributions.</summary>
public sealed class ContributionStore(TheOneDbContext db) : IContributionStore
{
    /// <inheritdoc />
    public async Task<Contribution> CreateAsync(Contribution contribution, CancellationToken ct)
    {
        db.Contributions.Add(contribution);
        await db.SaveChangesAsync(ct);
        return contribution;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Contribution>> ListByMemberAsync(Guid memberId, CancellationToken ct) =>
        await db.Contributions.Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.RecordedAtUtc)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<bool> HasMembershipFeeAsync(Guid memberId, decimal minimumAmount, CancellationToken ct) =>
        db.Contributions.AnyAsync(x =>
            x.MemberId == memberId &&
            x.Type == ContributionType.MembershipFee &&
            x.Amount >= minimumAmount, ct);
}
