using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TheOne.Application.Administration;
using TheOne.Application.Authentication.DTOs;
using TheOne.Persistence.Data;
using Xunit;
using static TheOne.IntegrationTests.UserManagementTests;
namespace TheOne.IntegrationTests;

/// <summary>Exercises permission enforcement, dynamic navigation, protected roles and durable audits.</summary>
[Collection("PostgreSQL authentication")]
public sealed class AdministrationTests
{
    [Fact]
    public async Task Custom_permissions_require_MFA_and_changes_take_effect_without_new_tokens()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); Authorize(c, owner.Token);
        var role = await CreateRole(c, Permissions.UsersRead);
        var delegated = await Account(f, c, role.Name); Authorize(c, delegated.Token);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/audit")).StatusCode);
        Authorize(c, owner.Token);
        await Read<RoleResponse>(await c.PutAsJsonAsync("/api/v1/admin/roles/" + role.Id, new RoleRequest(role.Name, [])));
        Authorize(c, delegated.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/users")).StatusCode);
        Authorize(c, owner.Token);
        await Read<RoleResponse>(await c.PutAsJsonAsync("/api/v1/admin/roles/" + role.Id, new RoleRequest(role.Name, [Permissions.UsersRead])));
        var member = await Account(f, c);
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
            db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { UserId = member.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
        }
        Authorize(c, member.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    [Fact]
    public async Task Role_assignments_require_enrollment_revoke_sessions_and_protect_system_roles()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); var member = await Account(f, c);
        Authorize(c, owner.Token);
        var role = await CreateRole(c, Permissions.UsersRead);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsync($"/api/v1/admin/users/{member.Id}/roles/{role.Id}", null)).StatusCode);
        var enrolled = await Account(f, c, "Admin"); Authorize(c, owner.Token);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsync($"/api/v1/admin/users/{enrolled.Id}/roles/{role.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync("/api/v1/admin/roles/" + role.Id)).StatusCode);
        Authorize(c, enrolled.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        Authorize(c, owner.Token);
        var system = await Read<RoleResponse[]>(await c.GetAsync("/api/v1/admin/roles"));
        var super = system.Single(x => x.Name == "SuperAdmin");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.DeleteAsync("/api/v1/admin/roles/" + super.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/v1/admin/roles/" + super.Id, new RoleRequest("SuperAdmin", []))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.DeleteAsync($"/api/v1/admin/users/{owner.Id}/roles/{super.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.DeleteAsync($"/api/v1/admin/users/{enrolled.Id}/roles/{role.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.DeleteAsync("/api/v1/admin/roles/" + role.Id)).StatusCode);
    }

    [Fact]
    public async Task Role_validation_and_non_owner_configuration_access_are_rejected()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); Authorize(c, owner.Token);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/admin/roles", new RoleRequest("Super Admin", []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/admin/roles", new RoleRequest("Invalid", ["invented.permission"]))).StatusCode);
        var role = await CreateRole(c);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/admin/roles", new RoleRequest(role.Name, []))).StatusCode);
        var admin = await Account(f, c, "Admin"); Authorize(c, admin.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/menus")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/v1/admin/roles", new RoleRequest("Denied", []))).StatusCode);
    }

    [Fact]
    public async Task Menu_assignments_hide_disabled_ancestors_and_enforce_permissions()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); Authorize(c, owner.Token);
        var role = await CreateRole(c, Permissions.UsersRead);
        var account = await Account(f, c, role.Name); Authorize(c, owner.Token);
        var parentRequest = new MenuRequest("Workspace", "কর্মক্ষেত্র", "folder", null, null, 20, true, null, [role.Id]);
        var parent = await Read<MenuResponse>(await c.PostAsJsonAsync("/api/v1/admin/menus", parentRequest));
        var child = await Read<MenuResponse>(await c.PostAsJsonAsync("/api/v1/admin/menus", new MenuRequest("Accounts", null, "users",
            "/administration/users", parent.Id, 1, true, Permissions.UsersRead, [role.Id])));
        Authorize(c, account.Token);
        var navigation = await Read<NavigationNode[]>(await c.GetAsync("/api/v1/me/menus"));
        Assert.Equal(child.Id, Assert.Single(Assert.Single(navigation).Children).Id);
        Authorize(c, owner.Token);
        await Read<MenuResponse>(await c.PutAsJsonAsync("/api/v1/admin/menus/" + parent.Id, parentRequest with { Enabled = false }));
        Authorize(c, account.Token);
        Assert.Empty(await Read<NavigationNode[]>(await c.GetAsync("/api/v1/me/menus")));
        Authorize(c, owner.Token);
        await Read<MenuResponse>(await c.PutAsJsonAsync("/api/v1/admin/menus/" + parent.Id, parentRequest));
        await Read<RoleResponse>(await c.PutAsJsonAsync("/api/v1/admin/roles/" + role.Id, new RoleRequest(role.Name, [])));
        Authorize(c, account.Token);
        Assert.Empty(await Read<NavigationNode[]>(await c.GetAsync("/api/v1/me/menus")));
    }

    [Fact]
    public async Task Menu_cycles_unsafe_routes_missing_roles_and_parent_deletion_are_rejected()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); Authorize(c, owner.Token);
        var role = await CreateRole(c);
        var request = new MenuRequest("Menu", null, null, "/test", null, 1, true, null, [role.Id]);
        var parent = await Read<MenuResponse>(await c.PostAsJsonAsync("/api/v1/admin/menus", request));
        var child = await Read<MenuResponse>(await c.PostAsJsonAsync("/api/v1/admin/menus", request with { ParentId = parent.Id }));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/v1/admin/menus/" + parent.Id, request with { ParentId = child.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync("/api/v1/admin/menus/" + parent.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/admin/menus", request with { Route = "//evil.example" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/admin/menus", request with { RoleIds = [Guid.NewGuid()] })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync("/api/v1/admin/roles/" + role.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.DeleteAsync("/api/v1/admin/menus/" + child.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.DeleteAsync("/api/v1/admin/menus/" + parent.Id)).StatusCode);
    }

    [Fact]
    public async Task Audit_is_filtered_credential_free_and_database_append_only()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); var member = await Account(f, c);
        Authorize(c, owner.Token); var role = await CreateRole(c);
        Assert.Equal(HttpStatusCode.OK, (await Status(c, member.Id, false)).StatusCode);
        var page = await Read<AuditPage>(await c.GetAsync($"/api/v1/admin/audit?actorId={owner.Id}&action=Role.Created&pageSize=1"));
        var entry = Assert.Single(page.Items); Assert.Equal(role.Id.ToString(), entry.Target); Assert.Equal(owner.Id, entry.ActorId);
        Assert.DoesNotContain("password", entry.Details, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(owner.Token.AccessToken, entry.Details);
        var status = await Read<AuditPage>(await c.GetAsync($"/api/v1/admin/audit?actorId={owner.Id}&action=User.StatusChanged"));
        Assert.Contains(status.Items, x => x.Target == member.Id.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/admin/audit?pageSize=101")).StatusCode);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        Assert.True(await db.SecurityAuditEvents.AnyAsync(x => x.Action == "Authentication.Request"));
        Assert.True(await db.SecurityAuditEvents.AnyAsync(x => x.Action == "Authentication.Login" && x.ActorId == owner.Id));
        await Assert.ThrowsAsync<PostgresException>(() => db.SecurityAuditEvents.Where(x => x.Id == entry.Id).ExecuteDeleteAsync());
        await Assert.ThrowsAsync<PostgresException>(() => db.SecurityAuditEvents.Where(x => x.Id == entry.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Action, "Tampered")));
        Assert.True(await db.SecurityAuditEvents.AnyAsync(x => x.Id == entry.Id));
    }

    [Fact]
    public async Task Concurrent_duplicate_role_creation_has_one_winner_and_one_audit_entry()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); Authorize(c, owner.Token);
        var name = "Concurrent_" + Guid.NewGuid().ToString("N");
        var results = await Task.WhenAll(c.PostAsJsonAsync("/api/v1/admin/roles", new RoleRequest(name, [])), c.PostAsJsonAsync("/api/v1/admin/roles", new RoleRequest(name, [])));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.BadRequest);
        var audits = await Read<AuditPage>(await c.GetAsync($"/api/v1/admin/audit?actorId={owner.Id}&action=Role.Created"));
        Assert.Single(audits.Items);
    }

    [Fact]
    public async Task Delegated_manager_can_change_members_but_not_administrators()
    {
        using var f = new AuthenticationTests.AuthenticationFactory(); using var c = Client(f);
        var owner = await Account(f, c, "SuperAdmin"); Authorize(c, owner.Token);
        var role = await CreateRole(c, Permissions.UsersRead, Permissions.UsersManage);
        var manager = await Account(f, c, role.Name);
        var member = await Account(f, c);
        Authorize(c, manager.Token);
        Assert.Equal(HttpStatusCode.OK, (await Status(c, member.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Status(c, owner.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Status(c, manager.Id, false)).StatusCode);
        Authorize(c, owner.Token);
        await Read<RoleResponse>(await c.PutAsJsonAsync("/api/v1/admin/roles/" + role.Id, new RoleRequest(role.Name, [Permissions.UsersRead])));
        Authorize(c, manager.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await Status(c, member.Id, true)).StatusCode);
    }

    private static Task<RoleResponse> CreateRole(HttpClient c, params string[] grants) => Create(c, grants);
    private static async Task<RoleResponse> Create(HttpClient c, string[] grants) => await Read<RoleResponse>(await c.PostAsJsonAsync("/api/v1/admin/roles",
        new RoleRequest("Role_" + Guid.NewGuid().ToString("N"), grants)));
}
