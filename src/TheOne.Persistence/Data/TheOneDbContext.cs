using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheOne.Domain.Entities;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence.Data;

/// <summary>
/// Represents the primary database context for The One platform.
/// </summary>
public sealed class TheOneDbContext
    : IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid>
{
    /// <summary>
    /// Initializes a new instance of the database context.
    /// </summary>
    public TheOneDbContext(
        DbContextOptions<TheOneDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets persisted refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    /// <summary>Gets persisted OTP challenges and request history.</summary>
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    /// <summary>Gets password-proven MFA challenges.</summary>
    public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();
    /// <summary>Gets consumed authenticator code digests.</summary>
    public DbSet<UsedAuthenticatorCode> UsedAuthenticatorCodes => Set<UsedAuthenticatorCode>();

    /// <summary>Navigation definitions.</summary>
    public DbSet<NavigationMenu> NavigationMenus => Set<NavigationMenu>();
    /// <summary>Navigation visibility assignments.</summary>
    public DbSet<NavigationMenuRole> NavigationMenuRoles => Set<NavigationMenuRole>();
    /// <summary>Persisted security history.</summary>
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(TheOneDbContext).Assembly);
    }
}
