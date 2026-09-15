using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TheOne.Application.Membership;
using TheOne.Persistence.Data;

namespace TheOne.Persistence.Membership;

public sealed class MembershipTransaction(TheOneDbContext db) : IMembershipTransaction
{
    public async Task<T> ExecuteAsync<T>(string? referenceCode, Func<Task<T>> action, CancellationToken ct)
    {
        if (referenceCode is not null && (string.IsNullOrWhiteSpace(referenceCode) || (referenceCode.Length > 40 && !(referenceCode.StartsWith("operator:", StringComparison.Ordinal) && Guid.TryParseExact(referenceCode[9..], "D", out _)))))
            throw new MembershipApplicationNotFoundException();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            if (referenceCode is not null)
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({referenceCode}, 0))", ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw new MembershipConflictException("This payment reference or membership record already exists.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }
}
