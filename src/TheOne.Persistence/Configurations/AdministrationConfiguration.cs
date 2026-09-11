using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;
namespace TheOne.Persistence.Configurations;

/// <summary>Navigation and audit persistence rules.</summary>
public sealed class AdministrationConfiguration : IEntityTypeConfiguration<NavigationMenu>, IEntityTypeConfiguration<NavigationMenuRole>, IEntityTypeConfiguration<SecurityAuditEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NavigationMenu> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.LabelEn).HasMaxLength(100).IsRequired(); b.Property(x => x.LabelBn).HasMaxLength(100);
        b.Property(x => x.Icon).HasMaxLength(60); b.Property(x => x.Route).HasMaxLength(200); b.Property(x => x.RequiredPermission).HasMaxLength(100);
        b.HasOne<NavigationMenu>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ParentId, x.SortOrder });
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NavigationMenuRole> b)
    {
        b.HasKey(x => new { x.MenuId, x.RoleId });
        b.HasOne<NavigationMenu>().WithMany().HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<IdentityRole<Guid>>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.Target).HasMaxLength(200).IsRequired(); b.Property(x => x.Details).HasColumnType("jsonb");
        b.HasIndex(x => new { x.CreatedAtUtc, x.Id }); b.HasIndex(x => new { x.ActorId, x.CreatedAtUtc }); b.HasIndex(x => new { x.Action, x.CreatedAtUtc });
    }
}
