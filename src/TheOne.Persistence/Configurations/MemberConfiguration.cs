using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;

namespace TheOne.Persistence.Configurations;

/// <summary>Configures persistence rules for member records.</summary>
public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ApplicationId).IsUnique();
        builder.HasOne<MembershipApplication>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MembershipNumber).IsUnique();
        builder.HasIndex(x => x.LinkedUserId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.MembershipNumber).HasMaxLength(40);
    }
}

/// <summary>Configures persistence rules for contributions.</summary>
public sealed class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.ToTable("Contributions");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.MemberId);
        builder.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TransactionReference).IsUnique().HasFilter("\"TransactionReference\" IS NOT NULL");
        builder.HasIndex(x => new { x.MemberId, x.Type });

        builder.Property(x => x.Amount).HasPrecision(10, 2);
        builder.Property(x => x.TransactionReference).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(500);
    }
}
