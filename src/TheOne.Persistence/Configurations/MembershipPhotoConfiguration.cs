using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;
namespace TheOne.Persistence.Configurations;
public sealed class MembershipPhotoConfiguration : IEntityTypeConfiguration<MembershipPhoto>
{
    public void Configure(EntityTypeBuilder<MembershipPhoto> b)
    {
        b.ToTable("MembershipPhotos"); b.HasKey(x => x.ApplicationId);
        b.Property(x => x.ApplicationId).ValueGeneratedNever();
        b.Property(x => x.ContentType).HasMaxLength(30);
        b.HasOne<MembershipApplication>().WithOne().HasForeignKey<MembershipPhoto>(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}
