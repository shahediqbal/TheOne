using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace TheOne.IntegrationTests;

[Collection("PostgreSQL authentication")]
public sealed class BrowserAuthTests
{
    [Fact]
    public async Task Browser_cookie_is_secure_rotates_and_never_appears_in_json()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var setup = UserManagementTests.Client(f);
        var account = await UserManagementTests.Account(f, setup);
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        c.DefaultRequestHeaders.Add("Origin", "https://localhost"); c.DefaultRequestHeaders.Add("X-TheOne-Client", "web");
        var login = await c.PostAsJsonAsync("/api/v1/browser/auth/login", new { userNameOrMobile = account.Email, password = "UserManagementTest123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("__Secure-TheOneRefresh=", cookie); Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.Contains("httponly", cookie.ToLowerInvariant()); Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("data").TryGetProperty("refreshToken", out _));
        var refreshed = await c.PostAsJsonAsync("/api/v1/browser/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(cookie.Split(';')[0], Assert.Single(refreshed.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/v1/browser/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/browser/auth/refresh", new { })).StatusCode);
    }
    [Fact]
    public async Task Missing_client_header_or_foreign_origin_is_forbidden()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = UserManagementTests.Client(f);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/v1/browser/auth/refresh", new { })).StatusCode);
        c.DefaultRequestHeaders.Add("Origin", "https://untrusted.example"); c.DefaultRequestHeaders.Add("X-TheOne-Client", "web");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/v1/browser/auth/refresh", new { })).StatusCode);
    }
}
