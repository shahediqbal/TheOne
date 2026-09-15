using Microsoft.EntityFrameworkCore;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;

namespace TheOne.Persistence.Membership;

/// <summary>EF Core-backed persistence for membership applications.</summary>
public sealed class MembershipApplicationStore(TheOneDbContext db) : IMembershipApplicationStore
{
    /// <inheritdoc />
    public async Task<MembershipApplication> CreateDraftAsync(MembershipApplication application, CancellationToken ct)
    {
        db.MembershipApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    /// <inheritdoc />
    public Task<MembershipApplication?> FindByReferenceCodeAsync(string referenceCode, CancellationToken ct) =>
        db.MembershipApplications.SingleOrDefaultAsync(x => x.ReferenceCode == referenceCode, ct);

    /// <inheritdoc />
    public Task SaveAsync(MembershipApplication application, CancellationToken ct) =>
        db.SaveChangesAsync(ct);

    /// <inheritdoc />
    public async Task<string> NextReferenceCodeAsync(int year, CancellationToken ct)
    {
        var number = await db.Database.SqlQueryRaw<long>(
            "SELECT nextval('\"MembershipReferenceNumbers\"') AS \"Value\"").SingleAsync(ct);
        return $"SDR-{year}-{number:000000}";
    }
}
