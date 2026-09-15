using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TheOne.Persistence.Migrations
{
    public partial class AddMembershipModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<long>(name: "MembershipReferenceNumbers");
            migrationBuilder.CreateSequence<long>(name: "MembershipNumbers");
            migrationBuilder.CreateTable(name: "MembershipApplications", columns: table => new
            {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ResumeTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullNameBn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FullNameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FatherNameBn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FatherNameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MotherNameBn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MotherNameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NidNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PermanentAddressBn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PermanentAddressEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TemporaryAddressBn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TemporaryAddressEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MaritalStatus = table.Column<int>(type: "integer", nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    OccupationBn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccupationEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BloodGroup = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    EducationBn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EducationEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmergencyContactNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PurposeOfJoiningBn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PurposeOfJoiningEn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LifeGoalBn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LifeGoalEn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SpecialSkillsBn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SpecialSkillsEn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HelpCategories = table.Column<int>(type: "integer", nullable: false),
                    TimeCommitment = table.Column<int>(type: "integer", nullable: false),
                    ContributionIntent = table.Column<int>(type: "integer", nullable: false),
                    CurrentChallengeBn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CurrentChallengeEn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CodeOfConductAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CommittedSinceYear = table.Column<int>(type: "integer", nullable: true),
                    FormalMembershipDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TranslationProvenance = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_MembershipApplications", x => x.Id);
            });
            migrationBuilder.CreateIndex(name: "IX_MembershipApplications_ReferenceCode", table: "MembershipApplications", columns: new[] { "ReferenceCode" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_MembershipApplications_ReferenceCode_ContactNumber", table: "MembershipApplications", columns: new[] { "ReferenceCode", "ContactNumber" });
            migrationBuilder.CreateIndex(name: "IX_MembershipApplications_UserId", table: "MembershipApplications", columns: new[] { "UserId" });
            migrationBuilder.CreateIndex(name: "IX_MembershipApplications_Status", table: "MembershipApplications", columns: new[] { "Status" });
            migrationBuilder.CreateTable(name: "Members", columns: table => new
            {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryChannel = table.Column<int>(type: "integer", nullable: false),
                    EnteredByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    MembershipNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LinkedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_Members", x => x.Id);
                table.ForeignKey(name: "FK_Members_MembershipApplications_ApplicationId", column: x => x.ApplicationId, principalTable: "MembershipApplications", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateIndex(name: "IX_Members_ApplicationId", table: "Members", columns: new[] { "ApplicationId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Members_MembershipNumber", table: "Members", columns: new[] { "MembershipNumber" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Members_LinkedUserId", table: "Members", columns: new[] { "LinkedUserId" });
            migrationBuilder.CreateIndex(name: "IX_Members_Status", table: "Members", columns: new[] { "Status" });
            migrationBuilder.CreateTable(name: "Contributions", columns: table => new
            {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    TransactionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_Contributions", x => x.Id);
                table.ForeignKey(name: "FK_Contributions_Members_MemberId", column: x => x.MemberId, principalTable: "Members", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateIndex(name: "IX_Contributions_MemberId", table: "Contributions", columns: new[] { "MemberId" });
            migrationBuilder.CreateIndex(name: "IX_Contributions_MemberId_Type", table: "Contributions", columns: new[] { "MemberId", "Type" });
            migrationBuilder.CreateIndex(name: "IX_Contributions_TransactionReference", table: "Contributions", columns: new[] { "TransactionReference" }, unique: true, filter: "\"TransactionReference\" IS NOT NULL");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("Contributions");
            migrationBuilder.DropTable("Members");
            migrationBuilder.DropTable("MembershipApplications");
            migrationBuilder.DropSequence("MembershipNumbers");
            migrationBuilder.DropSequence("MembershipReferenceNumbers");
        }
    }
}
