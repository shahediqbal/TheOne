using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Configurations;

/// <summary>Maps OTP challenges and their per-account request history.</summary>
public sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("OtpChallenges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProtectedCode).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SecurityStamp).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}