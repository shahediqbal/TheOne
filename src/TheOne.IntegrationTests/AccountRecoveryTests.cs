using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Common.Models;
using TheOne.Persistence.Data;
using Xunit;

namespace TheOne.IntegrationTests;

/// <summary>Exercises recovery with real Identity/PostgreSQL and a recording sender that never sends SMS.</summary>
[Collection("PostgreSQL authentication")]
public sealed class AccountRecoveryTests
{
    [Fact]
    public async Task Cancelled_delivery_does_not_roll_back_resend_limits()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        var account = await Account(client);
        var id = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .ReadJwtToken(account.Token.AccessToken).Subject);
        using var cancellation = new CancellationTokenSource();
        factory.Sms.BeforeSend = () => cancellation.Cancel();
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IAccountRecoveryService>();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.RequestMobileVerificationAsync(id, cancellation.Token));
        }
        factory.Sms.BeforeSend = null;
        using var nextScope = factory.Services.CreateScope();
        var nextService = nextScope.ServiceProvider.GetRequiredService<IAccountRecoveryService>();
        await Assert.ThrowsAsync<OtpRequestLimitException>(() => nextService.RequestMobileVerificationAsync(id));
        var challenge = await nextScope.ServiceProvider.GetRequiredService<TheOneDbContext>()
            .OtpChallenges.SingleAsync(x => x.UserId == id);
        Assert.NotNull(challenge.ConsumedAtUtc);
        Assert.Null(challenge.DeliveredAtUtc);
    }

    [Fact]
    public async Task Verified_mobile_can_reset_password_once_and_revoke_refresh_sessions()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        var account = await Account(client);
        var verification = await RequestVerification(client);
        var code = factory.Sms.Code;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            var stored = await db.OtpChallenges.SingleAsync(x => x.Id == verification.ChallengeId);
            Assert.NotEqual(code, stored.ProtectedCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await Verify(client, verification.ChallengeId, code)).StatusCode);
        factory.Clock.Advance(TimeSpan.FromSeconds(61));
        var reset = await Forgot(client, account.Email);
        var resetCode = factory.Sms.Code;
        var request = Reset(reset.ChallengeId, resetCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/reset-password", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/reset-password", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh-token",
            new RefreshTokenRequest { RefreshToken = account.Token.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = account.Email, Password = "TestPassword123" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = account.Email, Password = "RecoveredPassword456" })).StatusCode);
    }

    [Fact]
    public async Task Five_wrong_codes_exhaust_challenge_and_resend_supersedes_it()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        await Account(client);
        var first = await RequestVerification(client);
        var correct = factory.Sms.Code;
        var wrong = correct == "000000" ? "000001" : "000000";
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await Verify(client, first.ChallengeId, wrong)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Verify(client, first.ChallengeId, correct)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsync("/api/v1/auth/request-mobile-verification", null)).StatusCode);
        factory.Clock.Advance(TimeSpan.FromSeconds(61));
        var second = await RequestVerification(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await Verify(client, first.ChallengeId, correct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Verify(client, second.ChallengeId, factory.Sms.Code)).StatusCode);
    }

    [Fact]
    public async Task Expired_codes_and_cross_purpose_codes_are_rejected()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        await Account(client);
        var challenge = await RequestVerification(client);
        var code = factory.Sms.Code;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            Reset(challenge.ChallengeId, code))).StatusCode);
        factory.Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(HttpStatusCode.BadRequest, (await Verify(client, challenge.ChallengeId, code)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_reset_consumes_code_only_once()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        var account = await Account(client);
        var verify = await RequestVerification(client);
        Assert.Equal(HttpStatusCode.OK, (await Verify(client, verify.ChallengeId, factory.Sms.Code)).StatusCode);
        factory.Clock.Advance(TimeSpan.FromSeconds(61));
        var challenge = await Forgot(client, account.Email);
        var reset = Reset(challenge.ChallengeId, factory.Sms.Code);
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            client.PostAsJsonAsync("/api/v1/auth/reset-password", reset)));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Recovery_does_not_send_to_unverified_or_unknown_accounts()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        var account = await Account(client);
        var known = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { UserNameOrMobile = account.Email });
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { UserNameOrMobile = "missing@example.test" });
        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal((await known.Content.ReadFromJsonAsync<ApiResponse<OtpChallengeResponse>>())!.Message,
            (await unknown.Content.ReadFromJsonAsync<ApiResponse<OtpChallengeResponse>>())!.Message);
        Assert.Empty(factory.Sms.Messages);
    }

    [Fact]
    public async Task Failed_delivery_invalidates_code_and_still_counts_toward_resend_limit()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        await Account(client);
        factory.Sms.Fail = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await client.PostAsync("/api/v1/auth/request-mobile-verification", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsync("/api/v1/auth/request-mobile-verification", null)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        var id = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .ReadJwtToken(client.DefaultRequestHeaders.Authorization!.Parameter).Subject);
        var challenge = await db.OtpChallenges.SingleAsync(x => x.UserId == id);
        Assert.NotNull(challenge.ConsumedAtUtc);
    }

    [Fact]
    public async Task Password_change_invalidates_outstanding_code()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        await Account(client);
        var challenge = await RequestVerification(client);
        var code = factory.Sms.Code;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "TestPassword123", NewPassword = "ChangedPassword456",
                ConfirmPassword = "ChangedPassword456"
            })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Verify(client, challenge.ChallengeId, code)).StatusCode);
    }

    [Fact]
    public async Task Hourly_request_limit_survives_resend_intervals()
    {
        using var factory = new RecoveryFactory();
        using var client = factory.Client();
        await Account(client);
        for (var i = 0; i < 5; i++)
        {
            await RequestVerification(client);
            factory.Clock.Advance(TimeSpan.FromSeconds(61));
        }
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsync("/api/v1/auth/request-mobile-verification", null)).StatusCode);
        Assert.Equal(5, factory.Sms.Messages.Count);
    }

    [Fact]
    public async Task Verification_requires_JWT_and_cannot_confirm_another_account()
    {
        using var factory = new RecoveryFactory();
        using var first = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await first.PostAsync("/api/v1/auth/request-mobile-verification", null)).StatusCode);
        await Account(first);
        var challenge = await RequestVerification(first);
        var code = factory.Sms.Code;
        using var second = factory.Client();
        await Account(second);
        Assert.Equal(HttpStatusCode.BadRequest, (await Verify(second, challenge.ChallengeId, code)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Verify(first, challenge.ChallengeId, code)).StatusCode);
    }

    private static ResetPasswordRequest Reset(Guid id, string code) => new()
    {
        ChallengeId = id, Code = code, NewPassword = "RecoveredPassword456", ConfirmPassword = "RecoveredPassword456"
    };
    private static Task<HttpResponseMessage> Verify(HttpClient client, Guid id, string code) =>
        client.PostAsJsonAsync("/api/v1/auth/verify-mobile", new VerifyMobileRequest { ChallengeId = id, Code = code });
    private static async Task<OtpChallengeResponse> RequestVerification(HttpClient client)
    {
        var response = await client.PostAsync("/api/v1/auth/request-mobile-verification", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<OtpChallengeResponse>>())!.Data!;
    }
    private static async Task<OtpChallengeResponse> Forgot(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { UserNameOrMobile = email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<OtpChallengeResponse>>())!.Data!;
    }
    private static async Task<(string Email, TokenResponse Token)> Account(HttpClient client)
    {
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            FullName = "OTP Test", Email = email, MobileNumber = "017" + Random.Shared.Next(10000000, 99999999),
            Password = "TestPassword123", ConfirmPassword = "TestPassword123"
        })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserNameOrMobile = email, Password = "TestPassword123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return (email, token);
    }

    private sealed class RecordingSms : ISmsSender
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public bool Fail { get; set; }
        public Action? BeforeSend { get; set; }
        public string Code => Regex.Match(Messages.Last(), @"[0-9]{6}$").Value;
        public void EnsureAvailable() { }
        public Task SendAsync(string number, string message, CancellationToken cancellationToken)
        {
            BeforeSend?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new OtpDeliveryException();
            Assert.Matches(@"^Your The One OTP is [0-9]{6}$", message);
            Messages.Enqueue(message);
            return Task.CompletedTask;
        }
    }
    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
    private sealed class RecoveryFactory : WebApplicationFactory<Program>
    {
        public RecordingSms Sms { get; } = new();
        public TestClock Clock { get; } = new();
        public HttpClient Client() => CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connection = Environment.GetEnvironmentVariable("THEONE_AUTH_TEST_CONNECTION")
                ?? throw new InvalidOperationException("Set THEONE_AUTH_TEST_CONNECTION to a disposable PostgreSQL database.");
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["Jwt:Issuer"] = "TheOne.Tests", ["Jwt:Audience"] = "TheOne.Tests",
                ["Jwt:SecretKey"] = "TestOnlySigningKeyWithAtLeast32Bytes123456789"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISmsSender>();
                services.AddSingleton<ISmsSender>(Sms);
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
            });
        }
        protected override Microsoft.Extensions.Hosting.IHost CreateHost(Microsoft.Extensions.Hosting.IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<TheOneDbContext>().Database.Migrate();
            return host;
        }
    }
}
