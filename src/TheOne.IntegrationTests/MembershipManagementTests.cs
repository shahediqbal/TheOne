using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
using Xunit;
namespace TheOne.IntegrationTests;

[Collection("PostgreSQL authentication")]
public sealed class MembershipManagementTests
{
    private static OperatorMembershipRequest Request() => new()
    {
        RequestId = Guid.NewGuid(), CodeOfConductAccepted = true, DeclarationAccepted = true, OathAccepted = true,
        ConsentVersion = MembershipFormDefinition.Version, PhotoBase64 = MembershipFormFixture.Photo,
        Fields = MembershipFormFixture.Fields()
    };
    [Fact]
    public async Task Paper_entry_is_submitted_with_actor_and_retry_does_not_duplicate()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); var request = Request(); var staff = Guid.NewGuid();
        async Task<MembershipDetail> Enter()
        {
            using var scope = f.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IMembershipManagement>().EnterAsync(staff, request, default);
        }
        var a = await Enter(); var b = await Enter();
        Assert.Equal(a.Application.ReferenceCode, b.Application.ReferenceCode);
        request.Fields.FullNameEn = "Changed after commit";
        await Assert.ThrowsAsync<MembershipConflictException>(() => Enter());
        Assert.Equal(MembershipApplicationStatus.Submitted, a.Application.Status);
        Assert.Equal(MembershipEntryChannel.Operator, a.EntryChannel); Assert.Equal(staff, a.EnteredByStaffId);
        Assert.Null(a.Member!.MembershipNumber); Assert.Equal(MemberStatus.PendingApproval, a.Member.Status);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        Assert.Equal(1, await db.Members.CountAsync(x => x.ApplicationId == request.RequestId));
    }
    [Fact]
    public async Task Invalid_paper_form_leaves_no_application_or_member()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var scope = f.Services.CreateScope();
        var request = Request(); request.Fields.PermanentAddressBn = "";
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => scope.ServiceProvider.GetRequiredService<IMembershipManagement>().EnterAsync(Guid.NewGuid(), request, default));
        var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        Assert.False(await db.MembershipApplications.AnyAsync(x => x.Id == request.RequestId));
        Assert.False(await db.Members.AnyAsync(x => x.ApplicationId == request.RequestId));
    }
    [Fact]
    public async Task Queue_filters_and_details_do_not_expose_resume_hash()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var scope = f.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMembershipManagement>();
        var a = await service.EnterAsync(Guid.NewGuid(), Request(), default);
        var page = await service.ListAsync(new(Search: a.Application.ReferenceCode, Status: MembershipApplicationStatus.Submitted), default);
        Assert.Single(page.Items);
        var json = System.Text.Json.JsonSerializer.Serialize(await service.DetailAsync(a.Application.ReferenceCode, default));
        Assert.DoesNotContain("ResumeToken", json);
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.ListAsync(new(Page: -1), default));
    }
    [Fact]
    public async Task Staff_read_endpoints_require_authorization()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = UserManagementTests.Client(f);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/admin/membership/applications")).StatusCode);
        var ordinary = await UserManagementTests.Account(f, c); UserManagementTests.Authorize(c, ordinary.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/membership/applications")).StatusCode);
    }
}
