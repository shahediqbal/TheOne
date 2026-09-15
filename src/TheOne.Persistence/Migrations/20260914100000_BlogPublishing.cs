using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TheOne.Persistence.Migrations;
public partial class BlogPublishing:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"BlogPosts", columns: table => new {
            Id=table.Column<Guid>(type:"uuid", nullable:false),
            CreatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone", nullable:false),
            CreatedBy=table.Column<Guid>(type:"uuid", nullable:false),
        }, constraints: table => {
            table.PrimaryKey("PK_BlogPosts", x => x.Id);
        });
        migrationBuilder.CreateTable(name:"BlogTranslations", columns: table => new {
            Id=table.Column<Guid>(type:"uuid", nullable:false),
            BlogPostId=table.Column<Guid>(type:"uuid", nullable:false),
            Language=table.Column<string>(type:"character varying(2)", nullable:false, maxLength:2),
            Status=table.Column<int>(type:"integer", nullable:false),
            Version=table.Column<int>(type:"integer", nullable:false),
            DraftJson=table.Column<string>(type:"jsonb", nullable:false),
            PublishedRevisionId=table.Column<Guid>(type:"uuid", nullable:true),
            ApprovedBy=table.Column<Guid>(type:"uuid", nullable:true),
            ApprovedAtUtc=table.Column<DateTime>(type:"timestamp with time zone", nullable:true),
            PublishedAtUtc=table.Column<DateTime>(type:"timestamp with time zone", nullable:true),
            UpdatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone", nullable:false),
        }, constraints: table => {
            table.PrimaryKey("PK_BlogTranslations", x => x.Id);
            table.ForeignKey("FK_BlogTranslations_BlogPosts_BlogPostId",x=>x.BlogPostId,"BlogPosts","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_BlogTranslations_BlogPostId_Language","BlogTranslations",new[]{"BlogPostId","Language"},unique:true);
        migrationBuilder.CreateTable(name:"BlogRevisions", columns: table => new {
            Id=table.Column<Guid>(type:"uuid", nullable:false),
            TranslationId=table.Column<Guid>(type:"uuid", nullable:false),
            Version=table.Column<int>(type:"integer", nullable:false),
            DocumentJson=table.Column<string>(type:"jsonb", nullable:false),
            Action=table.Column<string>(type:"character varying(30)", nullable:false, maxLength:30),
            ActorId=table.Column<Guid>(type:"uuid", nullable:false),
            CreatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone", nullable:false),
        }, constraints: table => {
            table.PrimaryKey("PK_BlogRevisions", x => x.Id);
            table.ForeignKey("FK_BlogRevisions_BlogTranslations_TranslationId",x=>x.TranslationId,"BlogTranslations","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_BlogRevisions_TranslationId_Version","BlogRevisions",new[]{"TranslationId","Version"},unique:true);
        migrationBuilder.CreateTable(name:"BlogSlugs", columns: table => new {
            Language=table.Column<string>(type:"character varying(2)", nullable:false, maxLength:2),
            Slug=table.Column<string>(type:"character varying(180)", nullable:false, maxLength:180),
            TranslationId=table.Column<Guid>(type:"uuid", nullable:false),
        }, constraints: table => {
            table.PrimaryKey("PK_BlogSlugs", x => new {x.Language,x.Slug});
            table.ForeignKey("FK_BlogSlugs_BlogTranslations_TranslationId",x=>x.TranslationId,"BlogTranslations","Id",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_BlogSlugs_TranslationId","BlogSlugs","TranslationId");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("BlogSlugs");
        migrationBuilder.DropTable("BlogRevisions");
        migrationBuilder.DropTable("BlogTranslations");
        migrationBuilder.DropTable("BlogPosts");
    }
}
