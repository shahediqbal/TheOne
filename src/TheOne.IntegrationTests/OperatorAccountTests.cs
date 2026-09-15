using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TheOne.Application.Authentication.DTOs;
using TheOne.Application.Operations;
using TheOne.Domain.Entities;
using TheOne.Operations;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;
using TheOne.Persistence.Operations;
using Xunit;
using static TheOne.IntegrationTests.UserManagementTests;
namespace TheOne.IntegrationTests;

/// <summary>Operator flows are exercised through services and real API re-enrollment, never against a live account.</summary>
[Collection("PostgreSQL authentication")]
public sealed class OperatorAccountTests
{
    [Fact]
    public async Task Bootstrap_requires_enrollment_and_can_only_succeed_once()
    {
        using var f = new EmptyDatabaseFactory(); using var c = Client(f);
        var member = await Account(f, c);
        await WithService(f, async service =>
        {
            await Assert.ThrowsAsync<OperatorException>(() => service.BootstrapAsync(Approval(member.Id, member.Email), default));
        });
        await EnrollFixture(f, member.Id);
        await WithService(f, async service =>
        {
            var before = await service.InspectAsync(member.Id, member.Email, default);
            Assert.Equal(0, before.SuperAdminCount);
            var result = await service.BootstrapAsync(Approval(member.Id, member.Email), default);
            Assert.Equal(member.Id, result.UserId); Assert.Equal(1, result.RevokedSessions);
            await Assert.ThrowsAsync<OperatorException>(() => service.BootstrapAsync(Approval(member.Id, member.Email), default));
        });
        Authorize(c, member.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        Assert.Single(await db.SecurityAuditEvents.Where(x => x.Action == "Operator.SuperAdminBootstrapped").ToArrayAsync());
        Assert.Equal(1, await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur,r) => new { ur.UserId, r.Name }).CountAsync(x => x.Name == "SuperAdmin"));
    }

    [Fact]
    public async Task Concurrent_first_admin_bootstraps_have_only_one_winner()
    {
        using var f = new EmptyDatabaseFactory(); using var c = Client(f);
        var one = await Account(f, c); var two = await Account(f, c);
        await EnrollFixture(f, one.Id); await EnrollFixture(f, two.Id);
        async Task<bool> Try(Guid id, string email)
        {
            using var scope = f.Services.CreateScope();
            try { await Service(scope.ServiceProvider).BootstrapAsync(Approval(id, email), default); return true; }
            catch (OperatorException) { return false; }
        }
        var results = await Task.WhenAll(Try(one.Id, one.Email), Try(two.Id, two.Email));
        Assert.Equal(1, results.Count(x => x));
    }

    [Fact]
    public async Task Recovery_revokes_credentials_and_requires_fresh_authenticator_before_admin_login()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var admin = await Account(f, c, "Admin");
        string oldKey; string oldRecovery;
        using (var scope = f.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(admin.Id.ToString()))!;
            oldKey = (await users.GetAuthenticatorKeyAsync(user))!;
            oldRecovery = (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 1))!.Single();
            await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            db.OtpChallenges.Add(new OtpChallenge { Id=Guid.NewGuid(), UserId=user.Id, Purpose=OtpPurpose.PasswordReset,
                ProtectedCode="test-only", MobileNumber=user.PhoneNumber!, SecurityStamp=user.SecurityStamp!, CreatedAtUtc=DateTime.UtcNow, ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5) });
            await db.SaveChangesAsync();
        }
        OperatorRecoveryResult result = null!;
        await WithService(f, async service => result = await service.RecoverAsync(Approval(admin.Id, admin.Email), NewPassword, default));
        Authorize(c, admin.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/auth/refresh-token", new RefreshTokenRequest { RefreshToken=admin.Token.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(c, admin.Email)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { UserNameOrMobile=admin.Email, Password=NewPassword })).StatusCode);
        using (var scope = f.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(admin.Id.ToString()))!;
            Assert.False(user.TwoFactorEnabled); Assert.False(await users.IsLockedOutAsync(user));
            Assert.False((await users.RedeemTwoFactorRecoveryCodeAsync(user, oldRecovery)).Succeeded);
            Assert.False(await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, MfaLoginTests.Totp(oldKey)));
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            Assert.False(await db.OtpChallenges.AnyAsync(x => x.UserId == user.Id && x.ConsumedAtUtc == null));
        }
        var setup = await Read<AuthenticatorSetupResponse>(await c.PostAsJsonAsync("/api/v1/auth/authenticator/setup", new MfaChallengeRequest { ChallengeId=result.EnrollmentChallengeId }));
        Assert.NotEqual(oldKey, setup.SharedKey);
        var codes = await Read<RecoveryCodesResponse>(await c.PostAsJsonAsync("/api/v1/auth/authenticator/confirm-setup",
            new MfaCodeRequest { ChallengeId=result.EnrollmentChallengeId, Code=MfaLoginTests.Totp(setup.SharedKey, -1) }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/auth/authenticator/setup", new MfaChallengeRequest { ChallengeId=result.EnrollmentChallengeId })).StatusCode);
        var step = await Read<LoginResponse>(await c.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { UserNameOrMobile=admin.Email, Password=NewPassword }));
        var token = await Read<TokenResponse>(await c.PostAsJsonAsync("/api/v1/auth/login-with-recovery-code", new MfaRecoveryRequest { ChallengeId=step.ChallengeId!.Value, RecoveryCode=codes.RecoveryCodes.First() }));
        Authorize(c, token);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/admin/users")).StatusCode);
        using var auditScope = f.Services.CreateScope();
        var events = await auditScope.ServiceProvider.GetRequiredService<TheOneDbContext>().SecurityAuditEvents.Where(x => x.Target == admin.Id.ToString()).ToArrayAsync();
        var audit = Assert.Single(events, x=>x.Action=="Operator.AdminRecoveryStarted");
        Assert.Contains("OPS-TEST", audit.Details);
        Assert.DoesNotContain(NewPassword, audit.Details);
        Assert.DoesNotContain(result.EnrollmentChallengeId.ToString(), audit.Details);
        Assert.Contains(events, x=>x.Action=="Authentication.AuthenticatorEnrolled");
    }

    [Fact]
    public async Task Expired_recovery_can_be_reissued_without_reopening_old_challenge()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var admin = await Account(f,c,"Admin");
        OperatorRecoveryResult first=null!, second=null!;
        await WithService(f, async service=>first=await service.RecoverAsync(Approval(admin.Id,admin.Email),NewPassword,default));
        using(var scope=f.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<TheOneDbContext>().MfaChallenges.Where(x=>x.Id==first.EnrollmentChallengeId)
                .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ExpiresAtUtc,DateTime.UtcNow.AddMinutes(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/v1/auth/authenticator/setup",new MfaChallengeRequest{ChallengeId=first.EnrollmentChallengeId})).StatusCode);
        await WithService(f, async service=>second=await service.RecoverAsync(Approval(admin.Id,admin.Email),NewPassword,default));
        Assert.NotEqual(first.EnrollmentChallengeId,second.EnrollmentChallengeId);
        await Read<AuthenticatorSetupResponse>(await c.PostAsJsonAsync("/api/v1/auth/authenticator/setup",new MfaChallengeRequest{ChallengeId=second.EnrollmentChallengeId}));
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/v1/auth/authenticator/setup",new MfaChallengeRequest{ChallengeId=first.EnrollmentChallengeId})).StatusCode);
    }

    [Fact]
    public async Task Invalid_approval_wrong_target_and_ineligible_accounts_are_unchanged()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c=Client(f);
        var admin=await Account(f,c,"Admin");
        await WithService(f,async service =>
        {
            var request=Approval(admin.Id,admin.Email);
            await Assert.ThrowsAsync<OperatorException>(()=>service.RecoverAsync(request with { ApprovedBy=request.Operator },NewPassword,default));
            await Assert.ThrowsAsync<OperatorException>(()=>service.RecoverAsync(request with { IdentityVerified=false },NewPassword,default));
            await Assert.ThrowsAsync<OperatorException>(()=>service.RecoverAsync(request with { ExpectedEmail="wrong@example.test" },NewPassword,default));
            await Assert.ThrowsAsync<OperatorException>(()=>service.RecoverAsync(request,"short",default));
        });
        Authorize(c,admin.Token);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/v1/auth/me")).StatusCode);
        var member=await Account(f,c);
        await WithService(f,async service=>await Assert.ThrowsAsync<OperatorException>(()=>service.RecoverAsync(Approval(member.Id,member.Email),NewPassword,default)));
        using(var scope=f.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<TheOneDbContext>().Users.Where(x=>x.Id==admin.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsActive,false));
        await WithService(f,async service=>await Assert.ThrowsAsync<OperatorException>(()=>service.RecoverAsync(Approval(admin.Id,admin.Email),NewPassword,default)));
    }

    [Fact]
    public async Task Invalid_identity_password_policy_rolls_back_all_recovery_changes()
    {
        using var f=new AuthenticationTests.AuthenticationFactory(); using var c=Client(f);
        var admin=await Account(f,c,"Admin");
        await WithService(f,async service=>await Assert.ThrowsAsync<TheOne.Application.Authentication.AuthenticationException>(
            ()=>service.RecoverAsync(Approval(admin.Id,admin.Email),"abcdefghijklmnop",default)));
        Authorize(c,admin.Token);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/v1/auth/me")).StatusCode);
        using var scope=f.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        Assert.False(await db.SecurityAuditEvents.AnyAsync(x=>x.Action=="Operator.AdminRecoveryStarted" && x.Target==admin.Id.ToString()));
    }

    [Fact]
    public void Cli_defaults_to_inspection_and_rejects_password_arguments()
    {
        var args=new[]{"recover","--user-id",Guid.NewGuid().ToString(),"--email","account@example.test","--expect-host","localhost","--expect-port","55439","--expect-database","test"};
        Assert.False(OperatorCommand.Parse(args).Apply);
        Assert.Throws<OperatorException>(()=>OperatorCommand.Parse(args.Concat(new[]{"--password","must-not-be-accepted"}).ToArray()));
        Assert.Throws<OperatorException>(()=>OperatorCommand.Parse(args.Concat(new[]{"--expect-host","another"}).ToArray()));
        Assert.Throws<OperatorException>(()=>OperatorCommand.Parse(new[]{"recover"}));
    }

    private const string NewPassword="ReplacementPassword123!";
    private static OperatorRequest Approval(Guid id,string email)=>new(id,email,"operator-one","approver-two","OPS-TEST",true);
    private static OperatorAccountService Service(IServiceProvider services)=>new(services.GetRequiredService<TheOneDbContext>(),services.GetRequiredService<UserManager<ApplicationUser>>());
    private static async Task WithService(AuthenticationTests.AuthenticationFactory f,Func<OperatorAccountService,Task> operation)
    { using var scope=f.Services.CreateScope(); await operation(Service(scope.ServiceProvider)); }
    private static async Task EnrollFixture(AuthenticationTests.AuthenticationFactory f,Guid id)
    {
        using var scope=f.Services.CreateScope(); var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user=(await users.FindByIdAsync(id.ToString()))!; user.PhoneNumberConfirmed=true;
        Assert.True((await users.ResetAuthenticatorKeyAsync(user)).Succeeded);
        Assert.True((await users.SetTwoFactorEnabledAsync(user,true)).Succeeded);
    }

    private sealed class EmptyDatabaseFactory : AuthenticationTests.AuthenticationFactory
    {
        private readonly string connection;
        private readonly string control;
        private readonly string name="theone_ops_test_"+Guid.NewGuid().ToString("N");
        public EmptyDatabaseFactory()
        {
            var settings=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THEONE_AUTH_TEST_CONNECTION")
                ?? throw new InvalidOperationException("Test connection required."));
            if(settings.Host is not ("localhost" or "127.0.0.1") || settings.Port!=55439) throw new InvalidOperationException("Bootstrap tests require the isolated local cluster on port 55439.");
            settings.Database="postgres"; control=settings.ConnectionString;
            using var db=new NpgsqlConnection(control); db.Open();
            using var cmd=new NpgsqlCommand("CREATE DATABASE \""+name+"\"",db); cmd.ExecuteNonQuery();
            settings.Database=name; connection=settings.ConnectionString;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        { base.ConfigureWebHost(builder); builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["ConnectionStrings:DefaultConnection"]=connection})); }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(disposing)
            {
                using var db=new NpgsqlConnection(control); db.Open();
                using var cmd=new NpgsqlCommand("DROP DATABASE IF EXISTS \""+name+"\" WITH (FORCE)",db); cmd.ExecuteNonQuery();
            }
        }
    }
}
