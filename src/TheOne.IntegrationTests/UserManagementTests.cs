using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Common.Models;
using TheOne.Application.UserManagement;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;
using Xunit;

namespace TheOne.IntegrationTests;

/// <summary>Tests administrative account boundaries and session invalidation against PostgreSQL.</summary>
[Collection("PostgreSQL authentication")]
public sealed class UserManagementTests
{
    [Fact]
    public async Task Anonymous_and_members_cannot_access_administration()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/admin/users")).StatusCode);
        var member = await Account(f, c);
        Authorize(c, member.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/v1/admin/users/{member.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Status(c, member.Id, false)).StatusCode);
    }

    [Fact]
    public async Task Listing_searches_filters_pages_and_exposes_no_credentials()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var admin = await Account(f, c, "Admin");
        var member = await Account(f, c);
        Authorize(c, admin.Token);
        var response = await c.GetAsync($"/api/v1/admin/users?search={Uri.EscapeDataString(member.Email)}&role=Member&isActive=true&pageSize=1");
        var page = await Read<UserPageResponse>(response);
        Assert.Equal(1, page.TotalCount); Assert.Equal(member.Id, Assert.Single(page.Items).Id);
        var detail = await c.GetAsync($"/api/v1/admin/users/{member.Id}");
        Assert.Equal(member.Email, (await Read<ManagedUserResponse>(detail)).Email);
        var json = await detail.Content.ReadAsStringAsync();
        foreach (var secret in new[] { "passwordHash", "securityStamp", "recoveryCodes", "authenticatorKey", "refreshToken" }) Assert.DoesNotContain(secret, json);
        Assert.Empty((await Read<UserPageResponse>(await c.GetAsync($"/api/v1/admin/users?search={member.Email}&page=2&pageSize=1"))).Items);
        Assert.Empty((await Read<UserPageResponse>(await c.GetAsync("/api/v1/admin/users?search=%25%5F%25%5F"))).Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/admin/users?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/admin/users?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Deactivation_blocks_login_refresh_and_old_access_even_after_reactivation()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var admin = await Account(f, c, "Admin");
        var member = await Account(f, c);
        Authorize(c, admin.Token);
        Assert.Equal(HttpStatusCode.OK, (await Status(c, member.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Status(c, member.Id, false)).StatusCode);
        Assert.False((await Read<ManagedUserResponse>(await c.GetAsync($"/api/v1/admin/users/{member.Id}"))).IsActive);
        Authorize(c, member.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(c, member.Email)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/auth/refresh-token", new RefreshTokenRequest { RefreshToken = member.Token.RefreshToken })).StatusCode);
        Authorize(c, admin.Token);
        Assert.Equal(HttpStatusCode.OK, (await Status(c, member.Id, true)).StatusCode);
        Authorize(c, member.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        Authorize(c, await Read<TokenResponse>(await Login(c, member.Email)));
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_manage_peers_self_or_SuperAdmin()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var admin = await Account(f, c, "Admin");
        var peer = await Account(f, c, "Admin");
        var owner = await Account(f, c, "SuperAdmin");
        Authorize(c, admin.Token);
        foreach (var id in new[] { admin.Id, peer.Id, owner.Id })
            Assert.Equal(HttpStatusCode.Forbidden, (await Status(c, id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PatchAsJsonAsync($"/api/v1/admin/users/{peer.Id}/status", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Status(c, Guid.NewGuid(), false)).StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_can_manage_Admin_but_cannot_disable_SuperAdmins()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin");
        var admin = await Account(f, c, "Admin");
        var otherOwner = await Account(f, c, "SuperAdmin");
        Authorize(c, owner.Token);
        Assert.Equal(HttpStatusCode.OK, (await Status(c, admin.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Status(c, otherOwner.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Status(c, owner.Id, false)).StatusCode);
        Authorize(c, admin.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    [Fact]
    public async Task Revoked_administrator_session_cannot_change_accounts()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var admin = await Account(f, c, "Admin");
        var member = await Account(f, c);
        using (var scope = f.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<TheOneDbContext>().RefreshTokens.Where(x => x.UserId == admin.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow));
        Authorize(c, admin.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Status(c, member.Id, false)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_status_changes_are_serialized_and_old_session_stays_invalid()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var admin = await Account(f, c, "Admin");
        var member = await Account(f, c);
        Authorize(c, admin.Token);
        var results = await Task.WhenAll(Status(c, member.Id, false), Status(c, member.Id, true));
        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(HttpStatusCode.OK, (await Status(c, member.Id, true)).StatusCode);
        Authorize(c, member.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    private const string Password = "UserManagementTest123";
    internal static HttpClient Client(AuthenticationTests.AuthenticationFactory f) => f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    internal static void Authorize(HttpClient client, TokenResponse token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    internal static Task<HttpResponseMessage> Status(HttpClient c, Guid id, bool active) => c.PatchAsJsonAsync($"/api/v1/admin/users/{id}/status", new ChangeUserStatusRequest { IsActive = active });
    internal static Task<HttpResponseMessage> Login(HttpClient c, string email) => c.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { UserNameOrMobile = email, Password = Password });
    internal static async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<ApiResponse<T>>())!.Data!;
    }
    internal static async Task<(Guid Id, string Email, TokenResponse Token)> Account(AuthenticationTests.AuthenticationFactory f, HttpClient c, string? role = null)
    {
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        var account = await Read<RegisterResponse>(await c.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest {
            FullName = "User Management Test", Email = email, MobileNumber = "017" + Random.Shared.Next(10000000, 99999999), Password = Password, ConfirmPassword = Password }));
        if (role is null) return (account.UserId, email, await Read<TokenResponse>(await Login(c, email)));
        // Trusted fixture provisioning only; MFA enrollment behavior has its own end-to-end suite.
        string recoveryCode;
        using (var scope = f.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(account.UserId.ToString()))!;
            user.PhoneNumberConfirmed = true;
            Assert.True((await users.ResetAuthenticatorKeyAsync(user)).Succeeded);
            Assert.True((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
            recoveryCode = (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 1))!.Single();
        }
        var step = await Read<LoginResponse>(await Login(c, email));
        var token = await Read<TokenResponse>(await c.PostAsJsonAsync("/api/v1/auth/login-with-recovery-code",
            new MfaRecoveryRequest { ChallengeId = step.ChallengeId!.Value, RecoveryCode = recoveryCode }));
        return (account.UserId, email, token);
    }
}
