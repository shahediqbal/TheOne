using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TheOne.Persistence.Migrations;
/// <summary>Adds staff navigation through the existing permission and menu system.</summary>
public partial class AddStageStaffNavigation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "NavigationMenus" ("Id", "LabelEn", "LabelBn", "Icon", "Route", "ParentId", "SortOrder", "Enabled", "RequiredPermission")
            SELECT v.id::uuid, v.en, v.bn, v.icon, v.route, NULL, v.position, true, v.permission
            FROM (VALUES
                ('72000000-0000-4000-8000-000000000001','Membership','সদস্যপদ','users','/membership',20,'membership.read'),
                ('72000000-0000-4000-8000-000000000002','Blog publishing','ব্লগ প্রকাশনা','book','/blog',30,'blog.read'),
                ('72000000-0000-4000-8000-000000000003','Website content','ওয়েবসাইটের বিষয়বস্তু','globe','/website',40,'website.read')
            ) AS v(id,en,bn,icon,route,position,permission)
            WHERE NOT EXISTS (SELECT 1 FROM "NavigationMenus" m WHERE m."Route" = v.route OR m."Id" = v.id::uuid);
            INSERT INTO "NavigationMenuRoles" ("MenuId", "RoleId")
            SELECT m."Id", r."Id" FROM "NavigationMenus" m CROSS JOIN "AspNetRoles" r
            WHERE m."Id" IN ('72000000-0000-4000-8000-000000000001','72000000-0000-4000-8000-000000000002','72000000-0000-4000-8000-000000000003')
                AND r."NormalizedName" = 'SUPERADMIN'
            ON CONFLICT DO NOTHING;
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new System.NotSupportedException("Review a manual rollback: staff menus may have been customized.");
}
