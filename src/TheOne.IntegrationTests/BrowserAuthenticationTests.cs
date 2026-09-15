using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static TheOne.IntegrationTests.UserManagementTests;
namespace TheOne.IntegrationTests;
/// <summary>Checks browser cookie isolation and cross-origin protection.</summary>
[Collection("PostgreSQL authentication")]
public sealed class BrowserAuthenticationTests
{
    [Fact]
    public async Task Login_refresh_and_logout_keep_refresh_tokens_out_of_json()
    {
        using var f=new AuthenticationTests.AuthenticationFactory();
        using var c=f.CreateClient(new() { BaseAddress=new Uri("https://localhost"), HandleCookies=false });
        var account=await Account(f,c);
        c.DefaultRequestHeaders.Add("Origin","https://localhost"); c.DefaultRequestHeaders.Add("X-TheOne-Client","web");
        var response=await c.PostAsJsonAsync("/api/v1/browser/auth/login",new {userNameOrMobile=account.Email,password="UserManagementTest123"});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.DoesNotContain("refreshToken",await response.Content.ReadAsStringAsync(),StringComparison.OrdinalIgnoreCase);
        var cookie=Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly",cookie,StringComparison.OrdinalIgnoreCase); Assert.Contains("secure",cookie,StringComparison.OrdinalIgnoreCase); Assert.Contains("samesite=strict",cookie,StringComparison.OrdinalIgnoreCase);
        var credentials=cookie.Split(';')[0];
        c.DefaultRequestHeaders.Add("Cookie",credentials);
        var rotated=await c.PostAsJsonAsync("/api/v1/browser/auth/refresh",new {});
        Assert.Equal(HttpStatusCode.OK,rotated.StatusCode);
        Assert.DoesNotContain("refreshToken",await rotated.Content.ReadAsStringAsync(),StringComparison.OrdinalIgnoreCase);
        c.DefaultRequestHeaders.Remove("Cookie");
        var rotatedCookie=Assert.Single(rotated.Headers.GetValues("Set-Cookie")).Split(';')[0];
        Assert.NotEqual(credentials,rotatedCookie);
        c.DefaultRequestHeaders.Add("Cookie",rotatedCookie);
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/v1/browser/auth/logout",new {})).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/v1/browser/auth/refresh",new {})).StatusCode);
    }
    [Fact]
    public async Task Browser_endpoints_reject_missing_header_and_untrusted_origin()
    {
        using var f=new AuthenticationTests.AuthenticationFactory(); using var c=Client(f);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/v1/browser/auth/refresh",new {})).StatusCode);
        c.DefaultRequestHeaders.Add("Origin","https://untrusted.example"); c.DefaultRequestHeaders.Add("X-TheOne-Client","web");
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/v1/browser/auth/refresh",new {})).StatusCode);
    }
}
