using Microsoft.EntityFrameworkCore;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;

namespace TheOne.Persistence.Membership;

/// <summary>EF Core-backed persistence for member records.</summary>
public sealed class MemberStore(TheOneDbContext db) : IMemberStore
{
    /// <inheritdoc />
    public async Task<Member> CreateAsync(Member member, CancellationToken ct)
    {
        db.Members.Add(member);
        await db.SaveChangesAsync(ct);
        return member;
    }

    /// <inheritdoc />
    public Task<Member?> FindByApplicationIdAsync(Guid applicationId, CancellationToken ct) =>
        db.Members.SingleOrDefaultAsync(x => x.ApplicationId == applicationId, ct);

    /// <inheritdoc />
    public Task SaveAsync(Member member, CancellationToken ct) => db.SaveChangesAsync(ct);

    /// <inheritdoc />
    public async Task<string> NextMembershipNumberAsync(CancellationToken ct)
    {
        var number = await db.Database.SqlQueryRaw<long>(
            "SELECT nextval('\"MembershipNumbers\"') AS \"Value\"").SingleAsync(ct);
        return $"SDR-M-{number:000000}";
    }
}
