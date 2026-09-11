using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheOne.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticatorLoginPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Provision role names only; never create or promote an administrator account automatically.
            migrationBuilder.Sql(@"INSERT INTO ""AspNetRoles"" (""Id"", ""Name"", ""NormalizedName"", ""ConcurrencyStamp"")
                VALUES ('54838df1-3c37-4b2b-96ca-bfdd90b75bb8', 'Admin', 'ADMIN', '54838df1-3c37-4b2b-96ca-bfdd90b75bb8'),
                       ('e7dc7aab-4d99-412b-bd90-9d257fa76d90', 'SuperAdmin', 'SUPERADMIN', 'e7dc7aab-4d99-412b-bd90-9d257fa76d90')
                ON CONFLICT (""NormalizedName"") DO NOTHING;");
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationMethod",
                table: "RefreshTokens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MfaVerified",
                table: "RefreshTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RolesFingerprint",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "RefreshTokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MfaChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    SecurityStamp = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaChallenges_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsedAuthenticatorCodes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsedAuthenticatorCodes", x => new { x.UserId, x.CodeHash });
                    table.ForeignKey(
                        name: "FK_UsedAuthenticatorCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_UserId_CreatedAtUtc",
                table: "MfaChallenges",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MfaChallenges");

            migrationBuilder.DropTable(
                name: "UsedAuthenticatorCodes");

            migrationBuilder.DropColumn(
                name: "AuthenticationMethod",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "MfaVerified",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RolesFingerprint",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "RefreshTokens");
        }
    }
}
