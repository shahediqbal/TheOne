using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;
using Xunit;

namespace TheOne.IntegrationTests;

/// <summary>Tests password-plus-authenticator requirements and alternative login isolation without sending SMS.</summary>
[Collection("PostgreSQL authentication")]
public sealed class MfaLoginTests
{
    [Fact]
    public async Task Admin_password_returns_only_challenge_and_TOTP_completes_login()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        await Role(f, account.Id, LoginSecurityPolicy.Admin);
        var step = await PasswordStep(client, account.Email);
        Assert.True(step.RequiresTwoFactor);
        Assert.Null(step.AccessToken); Assert.Null(step.RefreshToken);
        Assert.False(step.RequiresAuthenticatorSetup);
        var tokens = await Read<TokenResponse>(await client.PostAsJsonAsync("/api/v1/auth/login-with-authenticator",
            new MfaCodeRequest { ChallengeId = step.ChallengeId!.Value, Code = Totp(account.Key) }));
        Assert.Contains(new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken).Claims, x => x.Type == "amr" && x.Value == "mfa");
        Authorize(client, tokens);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var refreshed = await Read<TokenResponse>(await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = tokens.RefreshToken }));
        Assert.Contains(new JwtSecurityTokenHandler().ReadJwtToken(refreshed.AccessToken).Claims, x => x.Type == "amr" && x.Value == "mfa");
    }

    [Fact]
    public async Task Enrollment_encrypts_key_hashes_recovery_codes_and_revokes_old_session()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        var values = await db.UserTokens.Where(x => x.UserId == account.Id).ToArrayAsync();
        Assert.StartsWith("dp:v1:", values.Single(x => x.Name == "AuthenticatorKey").Value);
        Assert.DoesNotContain(account.Key, values.Single(x => x.Name == "AuthenticatorKey").Value!);
        var recovery = values.Single(x => x.Name == "RecoveryCodes").Value!;
        Assert.All(account.Codes, code => Assert.DoesNotContain(code, recovery));
        Assert.Equal(10, account.Codes.Count);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = account.Original.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Unenrolled_admin_cannot_get_tokens_or_use_SMS_recovery()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Account(f, client);
        await Role(f, account.Id, LoginSecurityPolicy.SuperAdmin);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = account.Email, Password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        await Read<OtpChallengeResponse>(await client.PostAsJsonAsync("/api/v1/auth/request-login-otp",
            new SmsLoginRequest { MobileNumber = account.Mobile }));
        await Read<OtpChallengeResponse>(await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { UserNameOrMobile = account.Email }));
        Assert.Empty(f.Sms.Messages);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = account.Token.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Regular_user_can_login_by_SMS_once()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Account(f, client);
        var challenge = await Read<OtpChallengeResponse>(await client.PostAsJsonAsync("/api/v1/auth/request-login-otp",
            new SmsLoginRequest { MobileNumber = account.Mobile }));
        var request = new VerifyLoginOtpRequest { ChallengeId = challenge.ChallengeId, Code = f.Sms.Code };
        var token = await Read<TokenResponse>(await client.PostAsJsonAsync("/api/v1/auth/login-with-otp", request));
        Assert.Contains(new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken).Claims, x => x.Type == "amr" && x.Value == "sms");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/login-with-otp", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest { ChallengeId = challenge.ChallengeId, Code = request.Code,
                NewPassword = "OtherPassword456", ConfirmPassword = "OtherPassword456" })).StatusCode);
    }

    [Fact]
    public async Task Promotion_blocks_already_issued_SMS_code()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Account(f, client);
        var challenge = await Read<OtpChallengeResponse>(await client.PostAsJsonAsync("/api/v1/auth/request-login-otp",
            new SmsLoginRequest { MobileNumber = account.Mobile }));
        var code = f.Sms.Code;
        await Role(f, account.Id, LoginSecurityPolicy.Admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/login-with-otp",
            new VerifyLoginOtpRequest { ChallengeId = challenge.ChallengeId, Code = code })).StatusCode);
    }

    [Fact]
    public async Task MFA_challenge_and_authenticator_code_cannot_be_replayed()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        var first = await PasswordStep(client, account.Email);
        var code = Totp(account.Key);
        var request = new MfaCodeRequest { ChallengeId = first.ChallengeId!.Value, Code = code };
        await Read<TokenResponse>(await client.PostAsJsonAsync("/api/v1/auth/login-with-authenticator", request));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login-with-authenticator", request)).StatusCode);
        var next = await PasswordStep(client, account.Email);
        request.ChallengeId = next.ChallengeId!.Value;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login-with-authenticator", request)).StatusCode);
    }

    [Fact]
    public async Task Recovery_code_is_single_use_even_across_new_password_challenges()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        var first = await PasswordStep(client, account.Email);
        var request = new MfaRecoveryRequest { ChallengeId = first.ChallengeId!.Value, RecoveryCode = account.Codes[0] };
        await Read<TokenResponse>(await client.PostAsJsonAsync("/api/v1/auth/login-with-recovery-code", request));
        var next = await PasswordStep(client, account.Email);
        request.ChallengeId = next.ChallengeId!.Value;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login-with-recovery-code", request)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_MFA_completion_issues_only_one_session()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        var step = await PasswordStep(client, account.Email);
        var request = new MfaRecoveryRequest { ChallengeId = step.ChallengeId!.Value, RecoveryCode = account.Codes[0] };
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            client.PostAsJsonAsync("/api/v1/auth/login-with-recovery-code", request)));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Failed_second_factors_lock_account_without_password_step_resetting_failures()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        var step = await PasswordStep(client, account.Email);
        var wrong = Enumerable.Range(0, 100).Select(x => x.ToString("D6"))
            .First(x => !Enumerable.Range(-2, 5).Select(offset => Totp(account.Key, offset)).Contains(x));
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login-with-authenticator",
                new MfaCodeRequest { ChallengeId = step.ChallengeId!.Value, Code = wrong })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = account.Email, Password = Password })).StatusCode);
    }

    [Fact]
    public async Task Expired_and_wrong_purpose_challenges_do_not_issue_tokens()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        var step = await PasswordStep(client, account.Email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/authenticator/setup",
            new MfaChallengeRequest { ChallengeId = step.ChallengeId!.Value })).StatusCode);
        using (var scope = f.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<TheOneDbContext>().MfaChallenges
                .Where(x => x.Id == step.ChallengeId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login-with-authenticator",
            new MfaCodeRequest { ChallengeId = step.ChallengeId!.Value, Code = Totp(account.Key) })).StatusCode);
    }

    [Fact]
    public async Task Protected_reset_revokes_privileged_session_and_requires_reenrollment()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        await Role(f, account.Id, LoginSecurityPolicy.Admin);
        var tokens = await RecoveryLogin(client, account.Email, account.Codes[0]);
        Authorize(client, tokens);
        var reset = await Read<LoginResponse>(await client.PostAsJsonAsync("/api/v1/auth/authenticator/reset",
            new AuthenticatorResetRequest { Password = Password, RecoveryCode = account.Codes[1] }));
        Assert.True(reset.RequiresAuthenticatorSetup);
        Assert.Null(reset.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = account.Email, Password = Password })).StatusCode);
        var setup = await Read<AuthenticatorSetupResponse>(await client.PostAsJsonAsync("/api/v1/auth/authenticator/setup",
            new MfaChallengeRequest { ChallengeId = reset.ChallengeId!.Value }));
        Assert.NotEqual(account.Key, setup.SharedKey);
        await Read<RecoveryCodesResponse>(await client.PostAsJsonAsync("/api/v1/auth/authenticator/confirm-setup",
            new MfaCodeRequest { ChallengeId = reset.ChallengeId.Value, Code = Totp(setup.SharedKey, -1) }));
    }

    [Fact]
    public async Task SuperAdmin_promotion_requires_enrollment_and_revokes_targets_existing_MFA_session()
    {
        using var f = new Factory(); using var client = f.Client();
        var actor = await Enroll(f, client);
        await Role(f, actor.Id, LoginSecurityPolicy.SuperAdmin);
        var target = await Enroll(f, client);
        var targetToken = await RecoveryLogin(client, target.Email, target.Codes[0]);
        var actorToken = await RecoveryLogin(client, actor.Email, actor.Codes[0]);
        Authorize(client, actorToken);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/admin/users/{target.Id}/roles",
            new AssignAdministratorRoleRequest { Role = LoginSecurityPolicy.Admin })).StatusCode);
        Authorize(client, targetToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = targetToken.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Regular_user_cannot_assign_administrator_roles()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Account(f, client);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/v1/admin/users/{account.Id}/roles",
            new AssignAdministratorRoleRequest { Role = LoginSecurityPolicy.SuperAdmin })).StatusCode);
    }

    [Fact]
    public async Task Regenerating_recovery_codes_revokes_session_and_preserves_each_new_code()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        Authorize(client, await RecoveryLogin(client, account.Email, account.Codes[0]));
        var replacement = await Read<RecoveryCodesResponse>(await client.PostAsJsonAsync(
            "/api/v1/auth/authenticator/regenerate-recovery-codes",
            new AuthenticatorResetRequest { Password = Password, RecoveryCode = account.Codes[1] }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var step = await PasswordStep(client, account.Email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login-with-recovery-code",
            new MfaRecoveryRequest { ChallengeId = step.ChallengeId!.Value, RecoveryCode = account.Codes[2] })).StatusCode);
        foreach (var code in replacement.RecoveryCodes)
        {
            // Independent hosts avoid exhausting the per-IP limiter while exercising
            // all ten codes against the same persisted account and recovery-code set.
            using var loginHost = new Factory();
            using var loginClient = loginHost.Client();
            await RecoveryLogin(loginClient, account.Email, code);
        }
    }

    [Fact]
    public async Task SuperAdmin_cannot_promote_unenrolled_user()
    {
        using var f = new Factory(); using var client = f.Client();
        var actor = await Enroll(f, client);
        await Role(f, actor.Id, LoginSecurityPolicy.SuperAdmin);
        var target = await Account(f, client);
        Authorize(client, await RecoveryLogin(client, actor.Email, actor.Codes[0]));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/v1/admin/users/{target.Id}/roles",
            new AssignAdministratorRoleRequest { Role = LoginSecurityPolicy.Admin })).StatusCode);
    }

    [Fact]
    public async Task Protected_reset_requires_fresh_password_without_consuming_valid_recovery_code()
    {
        using var f = new Factory(); using var client = f.Client();
        var account = await Enroll(f, client);
        Authorize(client, await RecoveryLogin(client, account.Email, account.Codes[0]));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/authenticator/reset",
            new AuthenticatorResetRequest { Password = "WrongPassword123", RecoveryCode = account.Codes[1] })).StatusCode);
        await RecoveryLogin(client, account.Email, account.Codes[1]);
    }

    private const string Password = "TestPassword123";
    private static void Authorize(HttpClient client, TokenResponse token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<ApiResponse<T>>())!.Data!;
    }
    private static async Task<LoginResponse> PasswordStep(HttpClient client, string email) =>
        await Read<LoginResponse>(await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = email, Password = Password }));
    private static async Task<TokenResponse> RecoveryLogin(HttpClient client, string email, string code)
    {
        var step = await PasswordStep(client, email);
        return await Read<TokenResponse>(await client.PostAsJsonAsync("/api/v1/auth/login-with-recovery-code",
            new MfaRecoveryRequest { ChallengeId = step.ChallengeId!.Value, RecoveryCode = code }));
    }
    private static async Task<(Guid Id, string Email, string Mobile, TokenResponse Token)> Account(Factory f, HttpClient client)
    {
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        var mobile = "017" + Random.Shared.Next(10000000, 99999999);
        var registered = await Read<RegisterResponse>(await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest { FullName = "MFA Test", Email = email, MobileNumber = mobile,
                Password = Password, ConfirmPassword = Password }));
        var token = await Read<TokenResponse>(await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = email, Password = Password }));
        Authorize(client, token);
        using var scope = f.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TheOneDbContext>().Users.Where(x => x.Id == registered.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PhoneNumberConfirmed, true));
        return (registered.UserId, email, mobile, token);
    }
    private static async Task<(Guid Id, string Email, string Key, List<string> Codes, TokenResponse Original)> Enroll(Factory f, HttpClient client)
    {
        var account = await Account(f, client);
        var start = await Read<LoginResponse>(await client.PostAsJsonAsync("/api/v1/auth/authenticator/begin-enrollment",
            new PasswordConfirmationRequest { Password = Password }));
        var setup = await Read<AuthenticatorSetupResponse>(await client.PostAsJsonAsync("/api/v1/auth/authenticator/setup",
            new MfaChallengeRequest { ChallengeId = start.ChallengeId!.Value }));
        Assert.StartsWith("data:image/png;base64,", setup.QrCodeDataUri);
        Assert.StartsWith("otpauth://totp/", setup.AuthenticatorUri);
        var codes = await Read<RecoveryCodesResponse>(await client.PostAsJsonAsync("/api/v1/auth/authenticator/confirm-setup",
            new MfaCodeRequest { ChallengeId = start.ChallengeId.Value, Code = Totp(setup.SharedKey, -1) }));
        return (account.Id, account.Email, setup.SharedKey, codes.RecoveryCodes.ToList(), account.Token);
    }
    private static async Task Role(Factory f, Guid id, string role)
    {
        // Test-only trusted provisioning. Production assignments use the protected API.
        using var scope = f.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(id.ToString());
        Assert.True((await users.AddToRoleAsync(user!, role)).Succeeded);
    }

    private static string Totp(string key, int offset = 0)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>(); var buffer = 0; var bits = 0;
        foreach (var character in key.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character); bits += 5;
            if (bits >= 8) { bits -= 8; bytes.Add((byte)(buffer >> bits)); }
        }
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30 + offset);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter);
        var index = hash[^1] & 15;
        var value = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(index, 4)) & int.MaxValue;
        return (value % 1_000_000).ToString("D6");
    }
    private sealed class RecordingSms : ISmsSender
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public string Code => Regex.Match(Messages.Last(), @"[0-9]{6}$").Value;
        public void EnsureAvailable() { }
        public Task SendAsync(string number, string message, CancellationToken ct)
        { Messages.Enqueue(message); return Task.CompletedTask; }
    }
    private sealed class Factory : AuthenticationTests.AuthenticationFactory
    {
        public RecordingSms Sms { get; } = new();
        public HttpClient Client() => CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISmsSender>();
                services.AddSingleton<ISmsSender>(Sms);
            });
        }
    }
}
