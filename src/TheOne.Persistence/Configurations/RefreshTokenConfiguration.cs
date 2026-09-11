using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;

namespace TheOne.Persistence.Configurations;

/// <summary>
/// Configures persistence rules for refresh tokens.
/// </summary>
public sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthenticationMethod).HasMaxLength(32);
        builder.Property(x => x.SecurityStamp).HasMaxLength(256);
        builder.Property(x => x.RolesFingerprint).HasMaxLength(64);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.SessionId });

        builder.HasIndex(x => x.ExpiresAtUtc);

        builder.Property(x => x.CreatedByIp)
            .HasMaxLength(64);

        builder.Property(x => x.RevokedByIp)
            .HasMaxLength(64);

        builder.Property(x => x.RevocationReason)
            .HasMaxLength(500);

        builder.Property(x => x.ReplacedByTokenHash)
            .HasMaxLength(64);

        builder.Ignore(x => x.IsExpired);

        builder.Ignore(x => x.IsActive);
    }
}
