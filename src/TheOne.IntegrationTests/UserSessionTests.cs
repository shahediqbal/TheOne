using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Common.Models;
using TheOne.Persistence.Data;
using Xunit;

namespace TheOne.IntegrationTests;

/// <summary>Exercises ownership, stable session identity, and revocation against the real API.</summary>
[Collection("PostgreSQL authentication")]
public sealed class UserSessionTests
{
    [Fact]
    public async Task Profile_returns_safe_fields_and_sessions_identify_current_login()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var client = Client(factory);
        var account = await Account(client);
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserResponse>>())!.Data!;
        Assert.Equal(account.Email, profile.Email);
        Assert.Equal("Session Test", profile.FullName);
        Assert.Contains("Member", profile.Roles);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json);
        Assert.DoesNotContain("securityStamp", json);
        var page = await Sessions(client);
        var session = Assert.Single(page.Items);
        Assert.True(session.IsCurrent);
        Assert.Equal(SessionId(account.Token), session.SessionId);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Rotation_keeps_session_id_and_original_creation_time()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var client = Client(factory);
        var account = await Account(client);
        var initial = Assert.Single((await Sessions(client)).Items);
        var refreshed = await Token(await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = account.Token.RefreshToken }));
        Authorize(client, refreshed);
        var after = Assert.Single((await Sessions(client)).Items);
        Assert.Equal(initial.SessionId, after.SessionId);
        Assert.Equal(initial.CreatedAtUtc, after.CreatedAtUtc);
        Assert.True(after.IsCurrent);
        Assert.True(after.LastRefreshedAtUtc >= initial.LastRefreshedAtUtc);
        Assert.Equal(initial.SessionId, SessionId(refreshed));
    }

    [Fact]
    public async Task Revoking_one_session_preserves_other_logins_and_is_idempotent()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var client = Client(factory);
        var account = await Account(client);
        var second = await Login(client, account.Email);
        var id = SessionId(account.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/v1/auth/sessions/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/v1/auth/sessions/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = account.Token.RefreshToken })).StatusCode);
        await Token(await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = second.RefreshToken }));
        Assert.Single((await Sessions(client)).Items);
    }

    [Fact]
    public async Task Another_accounts_session_is_not_listed_or_revoked()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var first = Client(factory);
        using var second = Client(factory);
        var a = await Account(first);
        var b = await Account(second);
        var page = await Sessions(first);
        Assert.All(page.Items, x => Assert.Equal(SessionId(a.Token), x.SessionId));
        Assert.Equal(HttpStatusCode.OK,
            (await first.DeleteAsync("/api/v1/auth/sessions/" + SessionId(b.Token))).StatusCode);
        await Token(await second.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = b.Token.RefreshToken }));
    }

    [Fact]
    public async Task Logout_all_revokes_every_existing_session_and_allows_future_login()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var client = Client(factory);
        var account = await Account(client);
        var second = await Login(client, account.Email);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/auth/logout-all", null)).StatusCode);
        Assert.Empty((await Sessions(client)).Items);
        foreach (var token in new[] { account.Token, second })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
                new RefreshTokenRequest { RefreshToken = token.RefreshToken })).StatusCode);
        await Login(client, account.Email);
    }

    [Fact]
    public async Task Concurrent_refresh_and_revoke_leave_no_active_session()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var client = Client(factory);
        var account = await Account(client);
        var refresh = client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = account.Token.RefreshToken });
        var revoke = client.DeleteAsync("/api/v1/auth/sessions/" + SessionId(account.Token));
        await Task.WhenAll(refresh, revoke);
        var revokeResponse = await revoke;
        var refreshResponse = await refresh;
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
        Assert.Contains(refreshResponse.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        Assert.Empty((await Sessions(client)).Items);
        if (refreshResponse.IsSuccessStatusCode)
        {
            var rotated = await Token(refreshResponse);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
                new RefreshTokenRequest { RefreshToken = rotated.RefreshToken })).StatusCode);
        }
    }

    [Fact]
    public async Task Pagination_and_expiry_filtering_work()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var client = Client(factory);
        var account = await Account(client);
        var second = await Login(client, account.Email);
        var third = await Login(client, account.Email);
        var response = await client.GetAsync("/api/v1/auth/sessions?pageNumber=2&pageSize=1");
        var page = (await response.Content.ReadFromJsonAsync<ApiResponse<SessionListResponse>>())!.Data!;
        Assert.Equal(3, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(2, page.PageNumber);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            var id = SessionId(second);
            await db.RefreshTokens.Where(x => x.SessionId == id).ExecuteUpdateAsync(
                s => s.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }
        Assert.Equal(2, (await Sessions(client)).TotalCount);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/v1/auth/sessions?pageNumber=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/v1/auth/sessions?pageSize=101")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_and_inactive_accounts_cannot_use_profile_or_session_endpoints()
    {
        using var factory = new AuthenticationTests.AuthenticationFactory();
        using var client = Client(factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.DeleteAsync("/api/v1/auth/sessions/" + Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/logout-all", null)).StatusCode);
        var account = await Account(client);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            await db.Users.Where(x => x.Email == account.Email).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/logout-all", null)).StatusCode);
    }

    private static HttpClient Client(AuthenticationTests.AuthenticationFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    private static Guid SessionId(TokenResponse token) =>
        Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken).Claims.Single(x => x.Type == "sid").Value);
    private static void Authorize(HttpClient client, TokenResponse token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    private static async Task<(string Email, TokenResponse Token)> Account(HttpClient client)
    {
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            FullName = "Session Test", Email = email, MobileNumber = "017" + Random.Shared.Next(10000000, 99999999),
            Password = "TestPassword123", ConfirmPassword = "TestPassword123"
        })).StatusCode);
        var token = await Login(client, email);
        Authorize(client, token);
        return (email, token);
    }
    private static async Task<TokenResponse> Login(HttpClient client, string email) =>
        await Token(await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = email, Password = "TestPassword123" }));
    private static async Task<TokenResponse> Token(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>())!.Data!;
    }
    private static async Task<SessionListResponse> Sessions(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/sessions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("tokenHash", json);
        Assert.DoesNotContain("refreshToken", json);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<SessionListResponse>>())!.Data!;
    }
}
