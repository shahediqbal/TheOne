namespace TheOne.Application.Administration;
/// <summary>Stores request outcomes without credentials or request/response bodies.</summary>
public interface ISecurityRequestAudit
{
    /// <summary>Appends an authentication or access-denial result.</summary>
    Task RecordAsync(Guid? actor, string action, string route, int status, CancellationToken ct);
}
