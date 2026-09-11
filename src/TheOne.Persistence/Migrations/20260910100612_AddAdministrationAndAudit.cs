using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheOne.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrationAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NavigationMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LabelBn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredPermission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenus_NavigationMenus_ParentId",
                        column: x => x.ParentId,
                        principalTable: "NavigationMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Target = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NavigationMenuRoles",
                columns: table => new
                {
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenuRoles", x => new { x.MenuId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_NavigationMenuRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationMenuRoles_NavigationMenus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "NavigationMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuRoles_RoleId",
                table: "NavigationMenuRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_ParentId_SortOrder",
                table: "NavigationMenus",
                columns: new[] { "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_Action_CreatedAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "Action", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_ActorId_CreatedAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "ActorId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_CreatedAtUtc_Id",
                table: "SecurityAuditEvents",
                columns: new[] { "CreatedAtUtc", "Id" });
            migrationBuilder.Sql(@"INSERT INTO ""AspNetRoleClaims"" (""RoleId"", ""ClaimType"", ""ClaimValue"")
                SELECT r.""Id"", 'permission', p.value FROM ""AspNetRoles"" r
                CROSS JOIN (VALUES ('users.read'), ('users.manage'), ('audit.read')) p(value)
                WHERE r.""NormalizedName"" = 'ADMIN' AND NOT EXISTS
                (SELECT 1 FROM ""AspNetRoleClaims"" c WHERE c.""RoleId"" = r.""Id"" AND c.""ClaimType""='permission' AND c.""ClaimValue""=p.value);
                INSERT INTO ""NavigationMenus"" (""Id"", ""LabelEn"", ""LabelBn"", ""Icon"", ""Route"", ""ParentId"", ""SortOrder"", ""Enabled"", ""RequiredPermission"") VALUES
                ('71000000-0000-4000-8000-000000000001','Administration','প্রশাসন','settings',NULL,NULL,10,true,NULL),
                ('71000000-0000-4000-8000-000000000002','Users','ব্যবহারকারী','users','/administration/users','71000000-0000-4000-8000-000000000001',10,true,'users.read'),
                ('71000000-0000-4000-8000-000000000003','Roles and Permissions','ভূমিকা ও অনুমতি','shield','/administration/roles','71000000-0000-4000-8000-000000000001',20,true,NULL),
                ('71000000-0000-4000-8000-000000000004','Menu Management','মেনু ব্যবস্থাপনা','menu','/administration/menus','71000000-0000-4000-8000-000000000001',30,true,NULL),
                ('71000000-0000-4000-8000-000000000005','Audit History','অডিট ইতিহাস','history','/administration/audit','71000000-0000-4000-8000-000000000001',40,true,'audit.read');
                INSERT INTO ""NavigationMenuRoles"" (""MenuId"", ""RoleId"") SELECT m.""Id"", r.""Id""
                FROM ""NavigationMenus"" m CROSS JOIN ""AspNetRoles"" r
                WHERE r.""NormalizedName""='SUPERADMIN' OR (r.""NormalizedName""='ADMIN' AND m.""Id"" IN
                ('71000000-0000-4000-8000-000000000001','71000000-0000-4000-8000-000000000002','71000000-0000-4000-8000-000000000005'));
                CREATE FUNCTION theone_audit_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Security audit history is append-only'; END; $$;
                CREATE TRIGGER security_audit_immutable BEFORE UPDATE OR DELETE OR TRUNCATE ON ""SecurityAuditEvents""
                FOR EACH STATEMENT EXECUTE FUNCTION theone_audit_immutable();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NavigationMenuRoles");

            migrationBuilder.DropTable(
                name: "SecurityAuditEvents");

            migrationBuilder.DropTable(
                name: "NavigationMenus");
            migrationBuilder.Sql("DROP FUNCTION theone_audit_immutable();" );
        }
    }
}
