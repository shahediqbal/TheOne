using TheOne.Application.Administration;
using TheOne.Persistence.Data;
namespace TheOne.Persistence.Administration;
/// <summary>Writes request outcomes using a fresh request-independent context.</summary>
public sealed class SecurityRequestAudit(TheOneDbContext db) : ISecurityRequestAudit
{
    /// <inheritdoc />
    public async Task RecordAsync(Guid? actor, string action, string route, int status, CancellationToken ct)
    {
        AdministrationStore.Audit(db, actor, action, route, new { StatusCode = status });
        await db.SaveChangesAsync(ct);
    }
}
