using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TheOne.Persistence.Migrations;
public partial class CompleteWebsiteCms:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"WebsiteRecords",columns:table=>new {
            Id=table.Column<Guid>(type:"uuid",nullable:false),
            CanonicalId=table.Column<Guid>(type:"uuid",nullable:false),
            Title=table.Column<string>(type:"character varying(200)",nullable:false,maxLength:200),
            Kind=table.Column<string>(type:"character varying(30)",nullable:false,maxLength:30),
            Language=table.Column<string>(type:"character varying(2)",nullable:false,maxLength:2),
            LinkedStaffId=table.Column<Guid>(type:"uuid",nullable:true),
            Status=table.Column<int>(type:"integer",nullable:false),
            Version=table.Column<int>(type:"integer",nullable:false),
            DraftJson=table.Column<string>(type:"jsonb",nullable:false),
            PublishedRevisionId=table.Column<Guid>(type:"uuid",nullable:true),
            ApprovedBy=table.Column<Guid>(type:"uuid",nullable:true),
            ApprovedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:true),
            PublishedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:true),
            UpdatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),
        },constraints:table=>{
            table.PrimaryKey("PK_WebsiteRecords",x=>x.Id);
            table.ForeignKey("FK_WebsiteRecords_AspNetUsers_LinkedStaffId",x=>x.LinkedStaffId,"AspNetUsers","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_WebsiteRecords_CanonicalId_Language","WebsiteRecords",new[]{"CanonicalId","Language"},unique:true);
        migrationBuilder.CreateIndex("IX_WebsiteRecords_Kind_Language","WebsiteRecords",new[]{"Kind","Language"},unique:true,filter:"\"Kind\" = 'SiteSettings'");
        migrationBuilder.CreateIndex("IX_WebsiteRecords_LinkedStaffId","WebsiteRecords",new[]{"LinkedStaffId"},unique:false);
        migrationBuilder.CreateTable(name:"WebsiteRevisions",columns:table=>new {
            Id=table.Column<Guid>(type:"uuid",nullable:false),
            RecordId=table.Column<Guid>(type:"uuid",nullable:false),
            Version=table.Column<int>(type:"integer",nullable:false),
            DocumentJson=table.Column<string>(type:"jsonb",nullable:false),
            Action=table.Column<string>(type:"character varying(30)",nullable:false,maxLength:30),
            ActorId=table.Column<Guid>(type:"uuid",nullable:false),
            CreatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),
        },constraints:table=>{
            table.PrimaryKey("PK_WebsiteRevisions",x=>x.Id);
            table.ForeignKey("FK_WebsiteRevisions_WebsiteRecords_RecordId",x=>x.RecordId,"WebsiteRecords","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_WebsiteRevisions_RecordId_Version","WebsiteRevisions",new[]{"RecordId","Version"},unique:true);
        migrationBuilder.CreateTable(name:"WebsiteSlugs",columns:table=>new {
            Kind=table.Column<string>(type:"character varying(30)",nullable:false,maxLength:30),
            Language=table.Column<string>(type:"character varying(2)",nullable:false,maxLength:2),
            Slug=table.Column<string>(type:"character varying(180)",nullable:false,maxLength:180),
            RecordId=table.Column<Guid>(type:"uuid",nullable:false),
        },constraints:table=>{
            table.PrimaryKey("PK_WebsiteSlugs",x=>new{x.Kind,x.Language,x.Slug});
            table.ForeignKey("FK_WebsiteSlugs_WebsiteRecords_RecordId",x=>x.RecordId,"WebsiteRecords","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_WebsiteSlugs_RecordId","WebsiteSlugs",new[]{"RecordId"},unique:false);
        migrationBuilder.CreateTable(name:"WebsiteAssets",columns:table=>new {
            Id=table.Column<Guid>(type:"uuid",nullable:false),
            Content=table.Column<byte[]>(type:"bytea",nullable:false),
            ContentType=table.Column<string>(type:"character varying(30)",nullable:false,maxLength:30),
            OriginalName=table.Column<string>(type:"character varying(200)",nullable:false,maxLength:200),
            UploadedBy=table.Column<Guid>(type:"uuid",nullable:false),
            CreatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),
        },constraints:table=>{
            table.PrimaryKey("PK_WebsiteAssets",x=>x.Id);
        });
        migrationBuilder.CreateTable(name:"PublishingEvents",columns:table=>new {
            Id=table.Column<Guid>(type:"uuid",nullable:false),
            PayloadJson=table.Column<string>(type:"jsonb",nullable:false),
            CreatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),
            AcknowledgedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:true),
        },constraints:table=>{
            table.PrimaryKey("PK_PublishingEvents",x=>x.Id);
        });
        migrationBuilder.CreateIndex("IX_PublishingEvents_AcknowledgedAtUtc_CreatedAtUtc","PublishingEvents",new[]{"AcknowledgedAtUtc","CreatedAtUtc"},unique:false);
        migrationBuilder.CreateTable(name:"BlogRevisionAuthors",columns:table=>new {
            RevisionId=table.Column<Guid>(type:"uuid",nullable:false),
            AuthorId=table.Column<Guid>(type:"uuid",nullable:false),
            Position=table.Column<int>(type:"integer",nullable:false),
        },constraints:table=>{
            table.PrimaryKey("PK_BlogRevisionAuthors",x=>new{x.RevisionId,x.AuthorId});
            table.ForeignKey("FK_BlogRevisionAuthors_BlogRevisions_RevisionId",x=>x.RevisionId,"BlogRevisions","Id",onDelete:ReferentialAction.Restrict);
            table.ForeignKey("FK_BlogRevisionAuthors_WebsiteRecords_AuthorId",x=>x.AuthorId,"WebsiteRecords","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_BlogRevisionAuthors_AuthorId","BlogRevisionAuthors",new[]{"AuthorId"},unique:false);
        migrationBuilder.CreateTable(name:"BlogEmbeddings",columns:table=>new {
            TranslationId=table.Column<Guid>(type:"uuid",nullable:false),
            RevisionId=table.Column<Guid>(type:"uuid",nullable:false),
            Model=table.Column<string>(type:"character varying(100)",nullable:false,maxLength:100),
            EmbeddingData=table.Column<string>(type:"text",nullable:false),
            Dimensions=table.Column<int>(type:"integer",nullable:false),
        },constraints:table=>{
            table.PrimaryKey("PK_BlogEmbeddings",x=>x.TranslationId);
            table.ForeignKey("FK_BlogEmbeddings_BlogTranslations_TranslationId",x=>x.TranslationId,"BlogTranslations","Id",onDelete:ReferentialAction.Restrict);
            table.ForeignKey("FK_BlogEmbeddings_BlogRevisions_RevisionId",x=>x.RevisionId,"BlogRevisions","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_BlogEmbeddings_RevisionId","BlogEmbeddings",new[]{"RevisionId"},unique:false);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("BlogEmbeddings");
        migrationBuilder.DropTable("BlogRevisionAuthors");
        migrationBuilder.DropTable("PublishingEvents");
        migrationBuilder.DropTable("WebsiteAssets");
        migrationBuilder.DropTable("WebsiteSlugs");
        migrationBuilder.DropTable("WebsiteRevisions");
        migrationBuilder.DropTable("WebsiteRecords");
    }
}
