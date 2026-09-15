using TheOne.Application.Operations;
namespace TheOne.Operations;

/// <summary>Strict allowlisted command options. Passwords cannot be supplied as command arguments.</summary>
public sealed record OperatorCommand(string Action, Guid UserId, string Email, string Host, int Port, string Database, bool Apply,
    string? SecretsId, string? KeyRing, string ApplicationName, OperatorRequest Approval)
{
    /// <summary>Parses a dry-run by default; explicit destination confirmation is always required.</summary>
    public static OperatorCommand Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is not ("inspect" or "bootstrap" or "recover"))
            throw new OperatorException("Choose inspect, bootstrap or recover. Run with --help for usage.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string> { "--user-id", "--email", "--expect-host", "--expect-port", "--expect-database", "--secrets-id", "--key-ring", "--application-name", "--operator", "--approved-by", "--ticket" };
        for (var i = 1; i < args.Length; i++)
        {
            var name = args[i];
            if (name is "--apply" or "--identity-verified")
            {
                if (!flags.Add(name)) throw new OperatorException("Duplicate option.");
            }
            else if (!names.Contains(name) || i + 1 >= args.Length || args[i + 1].StartsWith("--") || !values.TryAdd(name, args[++i]))
                throw new OperatorException("Unknown, duplicate or missing option. Password arguments are never accepted.");
        }
        string Required(string name) => values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new OperatorException("Missing " + name);
        if (!Guid.TryParse(Required("--user-id"), out var id) || id == Guid.Empty) throw new OperatorException("A valid user ID is required.");
        var email = Required("--email"); var host = Required("--expect-host"); var database = Required("--expect-database");
        if (!int.TryParse(Required("--expect-port"), out var port) || port < 1 || port > 65535) throw new OperatorException("A valid expected database port is required.");
        var apply = flags.Contains("--apply");
        if (args[0] == "inspect" && apply) throw new OperatorException("Inspect cannot apply changes.");
        var secretsId = values.GetValueOrDefault("--secrets-id");
        if (secretsId is not null && !Guid.TryParse(secretsId, out _)) throw new OperatorException("Secrets ID must be a GUID.");
        var approval = new OperatorRequest(id, email, values.GetValueOrDefault("--operator") ?? "", values.GetValueOrDefault("--approved-by") ?? "",
            values.GetValueOrDefault("--ticket") ?? "", flags.Contains("--identity-verified"));
        return new(args[0], id, email, host, port, database, apply, secretsId, values.GetValueOrDefault("--key-ring"),
            values.GetValueOrDefault("--application-name") ?? "TheOne", approval);
    }
}
