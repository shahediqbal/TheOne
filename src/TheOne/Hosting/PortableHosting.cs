using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TheOne.Persistence.Data;

namespace TheOne.API.Hosting;

/// <summary>Host-independent production settings; provider addresses are supplied externally.</summary>
public static class PortableHosting
{
    /// <summary>Creates explicit, bounded proxy trust settings.</summary>
    public static ForwardedHeadersOptions ProxyOptions(IConfiguration config)
    {
        var result = new ForwardedHeadersOptions();
        if (!config.GetValue<bool>("ReverseProxy:Enabled")) return result;
        result.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        result.ForwardLimit = config.GetValue("ReverseProxy:ForwardLimit", 1);
        if (result.ForwardLimit is < 1 or > 5) throw new InvalidOperationException("ReverseProxy:ForwardLimit must be 1–5.");
        result.KnownProxies.Clear(); result.KnownIPNetworks.Clear();
        foreach (var value in config.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        {
            var address = IPAddress.Parse(value);
            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) throw new InvalidOperationException("Specify individual trusted proxy addresses.");
            result.KnownProxies.Add(address);
        }
        foreach (var value in config.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
        {
            var network = System.Net.IPNetwork.Parse(value);
            if (network.PrefixLength == 0) throw new InvalidOperationException("Trust-all proxy networks are forbidden.");
            result.KnownIPNetworks.Add(network);
        }
        if (result.KnownProxies.Count + result.KnownIPNetworks.Count == 0) throw new InvalidOperationException("Explicit trusted proxies/networks are required.");
        var hosts = (config["AllowedHosts"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0 || hosts.Any(h => h.Contains('*'))) throw new InvalidOperationException("Explicit AllowedHosts are required behind a proxy.");
        foreach (var host in hosts) result.AllowedHosts.Add(host);
        return result;
    }

    /// <summary>Registers durable key storage and health probes without changing the database.</summary>
    public static void AddPortableHosting(this WebApplicationBuilder builder)
    {
        if (builder.Configuration.GetValue<bool>("FORWARDEDHEADERS_ENABLED") ||
            string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED"), "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use ReverseProxy explicit trust settings instead of ASPNETCORE_FORWARDEDHEADERS_ENABLED.");
        var proxy = ProxyOptions(builder.Configuration);
        if (builder.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
        builder.Services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = proxy.ForwardedHeaders; options.ForwardLimit = proxy.ForwardLimit;
            options.KnownProxies.Clear(); options.KnownIPNetworks.Clear();
            foreach (var item in proxy.KnownProxies) options.KnownProxies.Add(item);
            foreach (var item in proxy.KnownIPNetworks) options.KnownIPNetworks.Add(item);
            foreach (var item in proxy.AllowedHosts) options.AllowedHosts.Add(item);
        });
        var directory = builder.Configuration["DataProtection:KeysPath"];
        if ((builder.Environment.IsProduction() || builder.Environment.IsStaging()) && string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("DataProtection:KeysPath must point to persistent storage outside the release directory.");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            if (!Path.IsPathFullyQualified(directory)) throw new InvalidOperationException("DataProtection:KeysPath must be absolute.");
            var protection = builder.Services.AddDataProtection().SetApplicationName("TheOne")
                .PersistKeysToFileSystem(new DirectoryInfo(directory));
            var certificatePath = builder.Configuration["DataProtection:CertificatePath"];
            if (!string.IsNullOrWhiteSpace(certificatePath))
                protection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(certificatePath, builder.Configuration["DataProtection:CertificatePassword"]));
        }
        builder.Services.AddHealthChecks().AddCheck<DatabaseReadiness>("database", tags: ["ready"]);
    }
}

/// <summary>Read-only database readiness. Does not apply migrations or expose connection details.</summary>
public sealed class DatabaseReadiness(IServiceScopeFactory scopes) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            return await db.Database.CanConnectAsync(timeout.Token) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch { return HealthCheckResult.Unhealthy(); }
    }
}
