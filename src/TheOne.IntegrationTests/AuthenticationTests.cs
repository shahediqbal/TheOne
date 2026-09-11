using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Common.Models;
using TheOne.Persistence.Data;
using Xunit;

namespace TheOne.IntegrationTests;

/// <summary>Exercises the real API, Identity stores, JWT validation, and PostgreSQL transactions.</summary>
[Collection("PostgreSQL authentication")]
public sealed class AuthenticationTests
{
    [Fact]
    public async Task Registration_login_rotation_and_logout_work()
    {
        using var factory = new AuthenticationFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var email = await RegisterAsync(client);
        var token = await LoginAsync(client, email);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            Assert.False(await db.RefreshTokens.AnyAsync(x => x.TokenHash == token.RefreshToken));
            var stored = await db.RefreshTokens.SingleAsync(x => x.UserId ==
                db.Users.Where(u => u.Email == email).Select(u => u.Id).Single());
            Assert.Equal(64, stored.TokenHash.Length);
        }
        var rotated = await ReadToken(await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = token.RefreshToken }));
        Assert.NotEqual(token.RefreshToken, rotated.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/logout",
            new RefreshTokenRequest { RefreshToken = rotated.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = rotated.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Replaying_rotated_token_revokes_replacement()
    {
        using var factory = new AuthenticationFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var original = await LoginAsync(client, await RegisterAsync(client));
        var replacement = await ReadToken(await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = original.RefreshToken }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = original.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = replacement.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Concurrent_refresh_has_only_one_success()
    {
        using var factory = new AuthenticationFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var token = await LoginAsync(client, await RegisterAsync(client));
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            client.PostAsJsonAsync("/api/v1/auth/refresh-token",
                new RefreshTokenRequest { RefreshToken = token.RefreshToken })));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Password_change_requires_JWT_and_revokes_all_sessions()
    {
        using var factory = new AuthenticationFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var email = await RegisterAsync(client);
        var first = await LoginAsync(client, email);
        var second = await LoginAsync(client, email);
        var change = new ChangePasswordRequest
        {
            CurrentPassword = "TestPassword123", NewPassword = "ChangedPassword456",
            ConfirmPassword = "ChangedPassword456"
        };
        var unauthenticated = await client.PostAsJsonAsync("/api/v1/auth/change-password", change);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.False((await unauthenticated.Content.ReadFromJsonAsync<ApiResponse<object>>())!.Success);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/v1/auth/change-password", change)).StatusCode);
        foreach (var session in new[] { first, second })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
                new RefreshTokenRequest { RefreshToken = session.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = email, Password = "TestPassword123" })).StatusCode);
        await LoginAsync(client, email, "ChangedPassword456");
    }

    [Fact]
    public async Task Five_failed_logins_lock_the_account()
    {
        using var factory = new AuthenticationFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var email = await RegisterAsync(client);
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest { UserNameOrMobile = email, Password = "WrongPassword123" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = email, Password = "TestPassword123" })).StatusCode);
    }

    [Fact]
    public async Task Invalid_registration_and_duplicate_mobile_are_rejected()
    {
        using var factory = new AuthenticationFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var invalid = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest());
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var response = await invalid.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(response!.Success);
        var mobile = NewMobile();
        await RegisterAsync(client, mobile);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/register",
            Registration(mobile))).StatusCode);
        await LoginAsync(client, mobile);
    }

    [Fact]
    public async Task Expired_tokens_and_inactive_accounts_are_rejected()
    {
        using var factory = new AuthenticationFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var email = await RegisterAsync(client);
        var token = await LoginAsync(client, email);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            var user = await db.Users.SingleAsync(x => x.Email == email);
            await db.RefreshTokens.Where(x => x.UserId == user.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = token.RefreshToken })).StatusCode);
        var activeToken = await LoginAsync(client, email);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            await db.Users.Where(x => x.Email == email).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = email, Password = "TestPassword123" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = activeToken.RefreshToken })).StatusCode);
    }

    private static string NewMobile() => "01" + Random.Shared.NextInt64(100000000, 999999999);
    private static RegisterRequest Registration(string? mobile = null) => new()
    {
        FullName = "Authentication Test", Email = Guid.NewGuid().ToString("N") + "@example.test",
        MobileNumber = mobile ?? NewMobile(), Password = "TestPassword123", ConfirmPassword = "TestPassword123"
    };
    private static async Task<string> RegisterAsync(HttpClient client, string? mobile = null)
    {
        var request = Registration(mobile);
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return request.Email;
    }
    private static async Task<TokenResponse> LoginAsync(HttpClient client, string identifier,
        string password = "TestPassword123") => await ReadToken(await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = identifier, Password = password }));
    private static async Task<TokenResponse> ReadToken(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        return (await response.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>())!.Data!;
    }

    internal class AuthenticationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connection = Environment.GetEnvironmentVariable("THEONE_AUTH_TEST_CONNECTION")
                ?? throw new InvalidOperationException("Set THEONE_AUTH_TEST_CONNECTION to a disposable PostgreSQL test database.");
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connection,
                    ["Jwt:Issuer"] = "TheOne.Tests", ["Jwt:Audience"] = "TheOne.Tests",
                    ["Jwt:SecretKey"] = "TestOnlySigningKeyWithAtLeast32Bytes123456789",
                    ["Jwt:AccessTokenExpirationMinutes"] = "15",
                    ["Jwt:RefreshTokenExpirationDays"] = "30"
                }));
        }

        protected override Microsoft.Extensions.Hosting.IHost CreateHost(
            Microsoft.Extensions.Hosting.IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<TheOneDbContext>().Database.Migrate();
            return host;
        }
    }
}
