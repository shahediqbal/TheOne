using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
using Xunit;

namespace TheOne.IntegrationTests;

/// <summary>Real PostgreSQL regression tests. Uses only the explicitly configured disposable test database.</summary>
[Collection("PostgreSQL authentication")]
public sealed class MembershipTests
{
    private const string Phone = "+8801700000000";
    // Every concurrent operation needs its own DbContext; never share a service scope between tasks.
    private static async Task<T> Run<T>(AuthenticationTests.AuthenticationFactory factory, Func<IServiceProvider, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }
    private static async Task<StartMembershipApplicationResponse> Draft(AuthenticationTests.AuthenticationFactory f, bool complete = true)
    {
        return await Run(f, async sp =>
        {
            var service = sp.GetRequiredService<IMembershipApplicationService>();
            var result = await service.StartAsync(new() { FullNameBn = "পরীক্ষা", FullNameEn = "Test Applicant", ContactNumber = Phone }, default);
            if (complete) {
                await service.SaveSectionAsync(result.ReferenceCode, MembershipFormFixture.Fields(result.ResumeToken), default);
                await sp.GetRequiredService<IMembershipPhotos>().SaveAsync(result.ReferenceCode, Phone, result.ResumeToken,
                    Convert.FromBase64String(MembershipFormFixture.Photo), default);
            }
            return result;
        });
    }
    private static Task<MembershipApplicationResponse> Submit(AuthenticationTests.AuthenticationFactory f, StartMembershipApplicationResponse a) =>
        Run(f, sp => sp.GetRequiredService<IMembershipApplicationService>().SubmitAsync(a.ReferenceCode,
            new() { ContactNumber = Phone, ResumeToken = a.ResumeToken, CodeOfConductAccepted = true, DeclarationAccepted = true, OathAccepted = true, ConsentVersion = MembershipFormDefinition.Version }, default));
    private static Task<MemberResponse> Verify(AuthenticationTests.AuthenticationFactory f, string reference, Guid staff) =>
        Run(f, sp => sp.GetRequiredService<IMembershipAdministrationService>().VerifyAsync(reference, staff, default));
    private static Task<MemberResponse> Approve(AuthenticationTests.AuthenticationFactory f, string reference, Guid staff) =>
        Run(f, sp => sp.GetRequiredService<IMembershipAdministrationService>().ApproveAsync(reference, staff, default));
    private static Task<ContributionResponse> Pay(AuthenticationTests.AuthenticationFactory f, string reference, decimal amount = 100m,
        string? transaction = null, ContributionType type = ContributionType.MembershipFee) =>
        Run(f, sp => sp.GetRequiredService<IMembershipAdministrationService>().RecordContributionAsync(reference, Guid.NewGuid(),
            new() { Type = type, Amount = amount, Method = transaction is null ? ContributionMethod.CashAtOffice : ContributionMethod.Bkash,
                TransactionReference = transaction }, default));

    [Fact]
    public async Task Resume_requires_private_token_even_when_reference_and_phone_are_known()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f);
        await Assert.ThrowsAsync<MembershipApplicationNotFoundException>(() => Run(f, sp =>
            sp.GetRequiredService<IMembershipApplicationService>().ResumeAsync(new()
                { ReferenceCode = a.ReferenceCode, ContactNumber = Phone, ResumeToken = new string('0', 64) }, default)));
        var state = await Run(f, sp => sp.GetRequiredService<IMembershipApplicationService>().ResumeAsync(new()
            { ReferenceCode = a.ReferenceCode, ContactNumber = Phone, ResumeToken = a.ResumeToken }, default));
        Assert.Equal("1234567890", state.NidNumber);
        var hash = await Run(f, sp => sp.GetRequiredService<TheOneDbContext>().MembershipApplications
            .Where(x => x.ReferenceCode == a.ReferenceCode).Select(x => x.ResumeTokenHash).SingleAsync());
        Assert.NotEqual(a.ResumeToken, hash);
        Assert.True(MembershipResumeCredential.Matches(a.ResumeToken, hash));
    }

    [Fact]
    public async Task Unauthorized_save_and_submit_leave_draft_unchanged()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f);
        await Assert.ThrowsAsync<MembershipApplicationNotFoundException>(() => Run(f, sp =>
            sp.GetRequiredService<IMembershipApplicationService>().SaveSectionAsync(a.ReferenceCode,
                new() { ContactNumber = Phone, ResumeToken = new string('0', 64), NidNumber = "999" }, default)));
        await Assert.ThrowsAsync<MembershipApplicationNotFoundException>(() => Submit(f, a with { ResumeToken = new string('0', 64) }));
        var stored = await Run(f, sp => sp.GetRequiredService<TheOneDbContext>().MembershipApplications.AsNoTracking()
            .SingleAsync(x => x.ReferenceCode == a.ReferenceCode));
        Assert.Equal(MembershipApplicationStatus.Draft, stored.Status);
        Assert.Equal("1234567890", stored.NidNumber);
    }

    [Fact]
    public async Task Incomplete_draft_cannot_submit_and_creates_no_member()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f, false);
        await Assert.ThrowsAsync<ValidationException>(() => Submit(f, a));
        await Run(f, async sp =>
        {
            var db = sp.GetRequiredService<TheOneDbContext>();
            var stored = await db.MembershipApplications.SingleAsync(x => x.ReferenceCode == a.ReferenceCode);
            Assert.Equal(MembershipApplicationStatus.Draft, stored.Status);
            Assert.False(await db.Members.AnyAsync(x => x.ApplicationId == stored.Id)); return true;
        });
    }

    [Fact]
    public async Task Failure_after_member_insert_rolls_back_entire_submission()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Run(f, sp =>
        {
            var service = new MembershipApplicationService(sp.GetRequiredService<IMembershipApplicationStore>(),
                new FailingMemberStore(sp.GetRequiredService<IMemberStore>()),
                sp.GetRequiredService<IMembershipTransaction>(), sp.GetRequiredService<MembershipSubmissionPolicy>());
            return service.SubmitAsync(a.ReferenceCode, new() { ContactNumber = Phone, ResumeToken = a.ResumeToken, CodeOfConductAccepted = true, DeclarationAccepted = true, OathAccepted = true, ConsentVersion = MembershipFormDefinition.Version }, default);
        }));
        await Run(f, async sp =>
        {
            var db = sp.GetRequiredService<TheOneDbContext>();
            var stored = await db.MembershipApplications.SingleAsync(x => x.ReferenceCode == a.ReferenceCode);
            Assert.Equal(MembershipApplicationStatus.Draft, stored.Status);
            Assert.Null(stored.SubmittedAtUtc);
            Assert.False(await db.Members.AnyAsync(x => x.ApplicationId == stored.Id)); return true;
        });
        await Submit(f, a); // A genuine retry succeeds after the injected failure is removed.
    }

    [Fact]
    public async Task Approval_requires_verification_and_qualifying_fee()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f); await Submit(f, a);
        await Assert.ThrowsAsync<MembershipConflictException>(() => Approve(f, a.ReferenceCode, Guid.NewGuid()));
        await Verify(f, a.ReferenceCode, Guid.NewGuid());
        await Pay(f, a.ReferenceCode, 500m, type: ContributionType.Donation);
        await Assert.ThrowsAsync<MembershipFeeNotPaidException>(() => Approve(f, a.ReferenceCode, Guid.NewGuid()));
        await Pay(f, a.ReferenceCode, 99m);
        await Assert.ThrowsAsync<MembershipFeeNotPaidException>(() => Approve(f, a.ReferenceCode, Guid.NewGuid()));
        await Pay(f, a.ReferenceCode);
        Assert.Equal(MemberStatus.Active, (await Approve(f, a.ReferenceCode, Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Concurrent_approval_preserves_one_number_and_original_actor()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f); await Submit(f, a);
        var verifier = Guid.NewGuid(); await Verify(f, a.ReferenceCode, verifier); await Pay(f, a.ReferenceCode);
        var staff1 = Guid.NewGuid(); var staff2 = Guid.NewGuid();
        var results = await Task.WhenAll(Approve(f, a.ReferenceCode, staff1), Approve(f, a.ReferenceCode, staff2));
        Assert.Equal(results[0].MembershipNumber, results[1].MembershipNumber);
        Assert.NotNull(results[0].MembershipNumber);
        var before = await Run(f, sp => sp.GetRequiredService<TheOneDbContext>().MembershipApplications.AsNoTracking()
            .SingleAsync(x => x.ReferenceCode == a.ReferenceCode));
        await Approve(f, a.ReferenceCode, Guid.NewGuid());
        var after = await Run(f, sp => sp.GetRequiredService<TheOneDbContext>().MembershipApplications.AsNoTracking()
            .SingleAsync(x => x.ReferenceCode == a.ReferenceCode));
        Assert.Equal(before.ApprovedBy, after.ApprovedBy); Assert.Equal(before.ApprovedAtUtc, after.ApprovedAtUtc);
        Assert.Equal(verifier, after.VerifiedBy);
    }

    [Fact]
    public async Task Concurrent_submit_creates_exactly_one_member()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f);
        async Task<Exception?> Attempt() { try { await Submit(f, a); return null; } catch (Exception ex) { return ex; } }
        var results = await Task.WhenAll(Attempt(), Attempt());
        Assert.Single(results.Where(x => x is null));
        Assert.Single(results.OfType<MembershipApplicationNotEditableException>());
        await Run(f, async sp =>
        {
            var db = sp.GetRequiredService<TheOneDbContext>(); var id = await db.MembershipApplications
                .Where(x => x.ReferenceCode == a.ReferenceCode).Select(x => x.Id).SingleAsync();
            Assert.Equal(1, await db.Members.CountAsync(x => x.ApplicationId == id)); return true;
        });
    }

    [Fact]
    public async Task Rejection_is_terminal_and_retains_first_reason_and_actor()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f); await Submit(f, a); await Pay(f, a.ReferenceCode);
        var actor = Guid.NewGuid();
        await Run(f, sp => sp.GetRequiredService<IMembershipAdministrationService>().RejectAsync(a.ReferenceCode, actor, new() { Reason = "Details could not be verified" }, default));
        await Run(f, sp => sp.GetRequiredService<IMembershipAdministrationService>().RejectAsync(a.ReferenceCode, Guid.NewGuid(), new() { Reason = "Replacement reason" }, default));
        await Assert.ThrowsAsync<MembershipConflictException>(() => Approve(f, a.ReferenceCode, actor));
        await Assert.ThrowsAsync<MembershipConflictException>(() => Verify(f, a.ReferenceCode, actor));
        var stored = await Run(f, sp => sp.GetRequiredService<TheOneDbContext>().MembershipApplications.AsNoTracking()
            .SingleAsync(x => x.ReferenceCode == a.ReferenceCode));
        Assert.Equal(actor, stored.RejectedBy); Assert.NotNull(stored.RejectedAtUtc);
        Assert.Equal("Details could not be verified", stored.RejectionReason);
    }

    [Fact]
    public async Task Bkash_reference_cannot_be_reused_across_members_or_case_variants()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var a = await Draft(f); var b = await Draft(f);
        await Submit(f, a); await Submit(f, b); var reference = Guid.NewGuid().ToString("N");
        await Pay(f, a.ReferenceCode, transaction: reference.ToLowerInvariant());
        await Assert.ThrowsAsync<MembershipConflictException>(() => Pay(f, b.ReferenceCode, transaction: " " + reference.ToUpperInvariant() + " "));
        var cash1 = await Pay(f, b.ReferenceCode); var cash2 = await Pay(f, b.ReferenceCode);
        Assert.NotEqual(cash1.Id, cash2.Id); // Cash has no external transaction ID; staff must review receipts.
    }

    [Fact]
    public async Task References_remain_unique_under_concurrency_and_after_draft_deletion()
    {
        using var f = new AuthenticationTests.AuthenticationFactory();
        var drafts = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Draft(f, false)));
        Assert.Equal(8, drafts.Select(x => x.ReferenceCode).Distinct().Count());
        await Run(f, async sp => await sp.GetRequiredService<TheOneDbContext>().MembershipApplications
            .Where(x => x.ReferenceCode == drafts[3].ReferenceCode).ExecuteDeleteAsync());
        var next = await Draft(f, false);
        Assert.DoesNotContain(next.ReferenceCode, drafts.Select(x => x.ReferenceCode));
    }

    [Fact]
    public async Task Member_number_allocator_is_unique_across_scopes()
    {
        using var f = new AuthenticationTests.AuthenticationFactory();
        var numbers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Run(f,
            sp => sp.GetRequiredService<IMemberStore>().NextMembershipNumberAsync(default))));
        Assert.Equal(8, numbers.Distinct().Count());
    }

    [Fact]
    public async Task Database_model_matches_migration_snapshot()
    {
        using var f = new AuthenticationTests.AuthenticationFactory();
        await Run(f, sp => Task.FromResult(AssertModel(sp)));
        static bool AssertModel(IServiceProvider sp)
        {
            Assert.False(sp.GetRequiredService<TheOneDbContext>().Database.HasPendingModelChanges()); return true;
        }
    }

    [Fact]
    public async Task Anonymous_staff_actions_are_denied_and_translation_is_disabled_by_default()
    {
        using var f = new AuthenticationTests.AuthenticationFactory();
        using var client = f.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/admin/membership/applications/unknown/approve", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/membership/translate", new { })).StatusCode);
    }

    [Fact]
    public async Task Membership_permissions_are_enforced_independently_of_login()
    {
        using var f = new AuthenticationTests.AuthenticationFactory();
        using var client = UserManagementTests.Client(f);
        var ordinary = await UserManagementTests.Account(f, client);
        UserManagementTests.Authorize(client, ordinary.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/admin/membership/applications/unknown/approve", new { })).StatusCode);
        var owner = await UserManagementTests.Account(f, client, "SuperAdmin");
        UserManagementTests.Authorize(client, owner.Token);
        var a = await Draft(f); await Submit(f, a);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/admin/membership/applications/{a.ReferenceCode}/verify", new { })).StatusCode);
    }

    private sealed class FailingMemberStore(IMemberStore inner) : IMemberStore
    {
        public async Task<Member> CreateAsync(Member member, CancellationToken ct)
        {
            await inner.CreateAsync(member, ct); throw new InvalidOperationException("Injected failure after insert.");
        }
        public Task<Member?> FindByApplicationIdAsync(Guid id, CancellationToken ct) => inner.FindByApplicationIdAsync(id, ct);
        public Task SaveAsync(Member member, CancellationToken ct) => inner.SaveAsync(member, ct);
        public Task<string> NextMembershipNumberAsync(CancellationToken ct) => inner.NextMembershipNumberAsync(ct);
    }
}
