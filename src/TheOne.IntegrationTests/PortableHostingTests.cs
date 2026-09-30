using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using TheOne.API.Hosting;
using Xunit;
namespace TheOne.IntegrationTests;
public class PortableHostingTests
{
    private static IConfiguration Config(Dictionary<string,string?> values) => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    [Fact] public void EnabledProxyNeedsExplicitTrust() => Assert.Throws<InvalidOperationException>(() => PortableHosting.ProxyOptions(Config(new() { ["ReverseProxy:Enabled"]="true" })));
    [Fact] public void RejectsTrustAllNetwork() => Assert.Throws<InvalidOperationException>(() => PortableHosting.ProxyOptions(Config(new() { ["ReverseProxy:Enabled"]="true", ["ReverseProxy:KnownNetworks:0"]="0.0.0.0/0", ["AllowedHosts"]="staff.example.org" })));
    [Theory]
    [InlineData("10.11.12.13", "https")]
    [InlineData("10.11.12.99", "http")]
    public async Task OnlyTrustedPeerCanChangeScheme(string peer,string expected)
    {
        var options=PortableHosting.ProxyOptions(Config(new() { ["ReverseProxy:Enabled"]="true", ["ReverseProxy:KnownProxies:0"]="10.11.12.13", ["AllowedHosts"]="staff.example.org" }));
        var context=new DefaultHttpContext();context.Connection.RemoteIpAddress=IPAddress.Parse(peer);context.Request.Scheme="http";
        context.Request.Headers["X-Forwarded-Proto"]="https";
        var middleware=new ForwardedHeadersMiddleware(_=>Task.CompletedTask,NullLoggerFactory.Instance,Options.Create(options));
        await middleware.Invoke(context);Assert.Equal(expected,context.Request.Scheme);
    }
}
