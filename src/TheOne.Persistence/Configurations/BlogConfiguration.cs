using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;
namespace TheOne.Persistence.Configurations;
public sealed class BlogConfiguration : IEntityTypeConfiguration<BlogPost>,IEntityTypeConfiguration<BlogPostTranslation>,IEntityTypeConfiguration<BlogRevision>,IEntityTypeConfiguration<BlogSlug>
{
 public void Configure(EntityTypeBuilder<BlogPost> b){b.ToTable("BlogPosts");b.HasKey(x=>x.Id);}
 public void Configure(EntityTypeBuilder<BlogPostTranslation> b){b.ToTable("BlogTranslations");b.HasKey(x=>x.Id);b.Property(x=>x.Version).IsConcurrencyToken();b.Property(x=>x.Language).HasMaxLength(2);b.Property(x=>x.DraftJson).HasColumnType("jsonb");b.HasIndex(x=>new{x.BlogPostId,x.Language}).IsUnique();b.HasOne<BlogPost>().WithMany().HasForeignKey(x=>x.BlogPostId).OnDelete(DeleteBehavior.Restrict);}
 public void Configure(EntityTypeBuilder<BlogRevision> b){b.ToTable("BlogRevisions");b.HasKey(x=>x.Id);b.Property(x=>x.DocumentJson).HasColumnType("jsonb");b.Property(x=>x.Action).HasMaxLength(30);b.HasIndex(x=>new{x.TranslationId,x.Version}).IsUnique();b.HasOne<BlogPostTranslation>().WithMany().HasForeignKey(x=>x.TranslationId).OnDelete(DeleteBehavior.Restrict);}
 public void Configure(EntityTypeBuilder<BlogSlug> b){b.ToTable("BlogSlugs");b.HasKey(x=>new{x.Language,x.Slug});b.Property(x=>x.Language).HasMaxLength(2);b.Property(x=>x.Slug).HasMaxLength(180);b.HasOne<BlogPostTranslation>().WithMany().HasForeignKey(x=>x.TranslationId).OnDelete(DeleteBehavior.Restrict);}
}
