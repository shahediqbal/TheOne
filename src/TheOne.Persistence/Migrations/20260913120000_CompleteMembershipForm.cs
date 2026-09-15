using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TheOne.Persistence.Migrations;
public partial class CompleteMembershipForm : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Earlier drafts accepted arbitrary PhotoUrl text. Require a genuine private upload after upgrading.
        migrationBuilder.Sql("UPDATE \"MembershipApplications\" SET \"PhotoUrl\" = NULL WHERE \"Status\" = 0");
            migrationBuilder.AddColumn<string>(name: "Email", table: "MembershipApplications", type: "character varying(254)", maxLength: 254, nullable: true);
            migrationBuilder.AddColumn<string>(name: "OtherHelpBn", table: "MembershipApplications", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "OtherHelpEn", table: "MembershipApplications", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "AboutSelfBn", table: "MembershipApplications", type: "character varying(2000)", maxLength: 2000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "AboutSelfEn", table: "MembershipApplications", type: "character varying(2000)", maxLength: 2000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SignatureName", table: "MembershipApplications", type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<DateOnly>(name: "ApplicationDate", table: "MembershipApplications", type: "date", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "DeclarationAccepted", table: "MembershipApplications", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "OathAccepted", table: "MembershipApplications", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<DateTime>(name: "ConsentAcceptedAtUtc", table: "MembershipApplications", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ConsentVersion", table: "MembershipApplications", type: "character varying(80)", maxLength: 80, nullable: true);
        migrationBuilder.CreateTable(name: "MembershipPhotos", columns: table => new {
            ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
            Content = table.Column<byte[]>(type: "bytea", nullable: false),
            ContentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_MembershipPhotos", x => x.ApplicationId);
            table.ForeignKey("FK_MembershipPhotos_MembershipApplications_ApplicationId", x => x.ApplicationId,
                "MembershipApplications", "Id", onDelete: ReferentialAction.Cascade);
        });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("MembershipPhotos");
        migrationBuilder.DropColumn("Email", "MembershipApplications");
        migrationBuilder.DropColumn("OtherHelpBn", "MembershipApplications");
        migrationBuilder.DropColumn("OtherHelpEn", "MembershipApplications");
        migrationBuilder.DropColumn("AboutSelfBn", "MembershipApplications");
        migrationBuilder.DropColumn("AboutSelfEn", "MembershipApplications");
        migrationBuilder.DropColumn("SignatureName", "MembershipApplications");
        migrationBuilder.DropColumn("ApplicationDate", "MembershipApplications");
        migrationBuilder.DropColumn("DeclarationAccepted", "MembershipApplications");
        migrationBuilder.DropColumn("OathAccepted", "MembershipApplications");
        migrationBuilder.DropColumn("ConsentAcceptedAtUtc", "MembershipApplications");
        migrationBuilder.DropColumn("ConsentVersion", "MembershipApplications");
    }
}
