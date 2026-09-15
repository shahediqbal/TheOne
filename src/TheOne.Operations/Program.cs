using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TheOne.Application.Authentication;
using TheOne.Application.Operations;
using TheOne.Persistence;
using TheOne.Persistence.Data;
using TheOne.Persistence.Operations;
namespace TheOne.Operations;

/// <summary>Operator-only entry point. Does not start an HTTP server.</summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args is ["--help"])
        {
            Console.WriteLine("TheOne operator CLI: inspect | bootstrap | recover");
            Console.WriteLine("Required: --user-id GUID --email EMAIL --expect-host HOST --expect-port PORT --expect-database DATABASE");
            Console.WriteLine("Connection: --secrets-id GUID, or protected environment THEONE_OPS_CONNECTION");
            Console.WriteLine("Optional: --key-ring PATH --application-name NAME (default TheOne)");
            Console.WriteLine("Changes require: --apply --operator NAME --approved-by OTHER-NAME --ticket REFERENCE --identity-verified");
            Console.WriteLine("Without --apply all commands inspect only. Recovery passwords are entered interactively without echo.");
            return 0;
        }
        try
        {
            var command = OperatorCommand.Parse(args);
            if (command.Apply && (Console.IsInputRedirected || Console.IsOutputRedirected))
                throw new OperatorException("Apply requires an interactive terminal. Do not pipe or capture recovery credentials.");
            var configuration = new ConfigurationBuilder();
            if (command.SecretsId is not null) configuration.AddUserSecrets(command.SecretsId);
            var config = configuration.Build();
            var connection = Environment.GetEnvironmentVariable("THEONE_OPS_CONNECTION") ?? config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connection)) throw new OperatorException("No database connection is configured.");
            var parsed = new NpgsqlConnectionStringBuilder(connection);
            if (parsed.Port != command.Port || !string.Equals(parsed.Host, command.Host, StringComparison.OrdinalIgnoreCase) || !string.Equals(parsed.Database, command.Database, StringComparison.Ordinal))
                throw new OperatorException("Configured database does not match the explicitly expected host/database. No changes made.");
            var services = new ServiceCollection();
            services.AddLogging(); // No console provider: EF must never print parameters or sensitive state.
            var protection = services.AddDataProtection().SetApplicationName(command.ApplicationName);
            if (command.KeyRing is not null)
            {
                if (!Directory.Exists(command.KeyRing)) throw new OperatorException("The specified key-ring directory does not exist.");
                protection.PersistKeysToFileSystem(new DirectoryInfo(command.KeyRing));
            }
            services.AddPersistence(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connection }).Build());
            services.AddScoped<IOperatorAccountService, OperatorAccountService>();
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new OperatorException("Apply pending migrations through normal deployment before running this command.");
            var service = scope.ServiceProvider.GetRequiredService<IOperatorAccountService>();
            var state = await service.InspectAsync(command.UserId, command.Email, CancellationToken.None);
            Console.WriteLine("Target: " + command.Host + ":" + command.Port + " / " + command.Database);
            Console.WriteLine(JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            if (!command.Apply)
            {
                Console.WriteLine("Inspection only. No account changes made. Approval and password checks run again on apply.");
                return 0;
            }
            if (command.Action == "bootstrap")
            {
                var result = await service.BootstrapAsync(command.Approval, CancellationToken.None);
                Console.WriteLine("First SuperAdmin created; revoked sessions: " + result.RevokedSessions + ". Sign in with password plus authenticator.");
            }
            else
            {
                Console.WriteLine("Approval must be obtained outside this tool. Stop terminal recording before continuing.");
                var password = ReadPassword("New password: ");
                if (password != ReadPassword("Confirm new password: ")) throw new OperatorException("Passwords do not match.");
                var result = await service.RecoverAsync(command.Approval, password, CancellationToken.None);
                Console.WriteLine("Recovery committed. Prior sessions and credentials invalidated. Administrator login remains blocked pending enrollment.");
                Console.WriteLine("CONFIDENTIAL enrollment challenge (expires in 5 minutes; do not log or share in chat):");
                Console.WriteLine(result.EnrollmentChallengeId);
                Console.WriteLine("Use authenticator/setup and authenticator/confirm-setup immediately. See docs/operator-recovery.md.");
            }
            return 0;
        }
        catch (OperatorException ex) { Console.Error.WriteLine(ex.Message); return 2; }
        catch (AuthenticationException ex) { Console.Error.WriteLine(ex.Message); return 2; }
        catch (Exception) { Console.Error.WriteLine("Operation failed. Verify database access, migrations and Data Protection configuration. Secret error details are intentionally suppressed."); return 1; }
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt); var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return buffer.ToString(); }
            if (key.Key == ConsoleKey.Escape) throw new OperatorException("Canceled.");
            if (key.Key == ConsoleKey.Backspace) { if (buffer.Length > 0) buffer.Length--; continue; }
            if (!char.IsControl(key.KeyChar))
            {
                if (buffer.Length >= 128) throw new OperatorException("Password is too long.");
                buffer.Append(key.KeyChar);
            }
        }
    }
}
