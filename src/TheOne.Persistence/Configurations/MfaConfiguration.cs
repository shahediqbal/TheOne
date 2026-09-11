using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Configurations;

/// <summary>Maps MFA password proofs and account ownership.</summary>
public sealed class MfaChallengeConfiguration : IEntityTypeConfiguration<MfaChallenge>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MfaChallenge> b)
    {
        b.ToTable("MfaChallenges");
        b.HasKey(x => x.Id);
        b.Property(x => x.SecurityStamp).HasMaxLength(256);
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
/// <summary>Maps short-lived authenticator replay protection.</summary>
public sealed class UsedAuthenticatorCodeConfiguration : IEntityTypeConfiguration<UsedAuthenticatorCode>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UsedAuthenticatorCode> b)
    {
        b.ToTable("UsedAuthenticatorCodes");
        b.HasKey(x => new { x.UserId, x.CodeHash });
        b.Property(x => x.CodeHash).HasMaxLength(64);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}