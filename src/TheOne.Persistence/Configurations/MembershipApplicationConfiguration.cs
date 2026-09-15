using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheOne.Domain.Entities;

namespace TheOne.Persistence.Configurations;

/// <summary>Configures persistence rules for membership applications.</summary>
public sealed class MembershipApplicationConfiguration : IEntityTypeConfiguration<MembershipApplication>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MembershipApplication> builder)
    {
        builder.ToTable("MembershipApplications");
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.OtherHelpBn).HasMaxLength(500);
        builder.Property(x => x.OtherHelpEn).HasMaxLength(500);
        builder.Property(x => x.AboutSelfBn).HasMaxLength(2000);
        builder.Property(x => x.AboutSelfEn).HasMaxLength(2000);
        builder.Property(x => x.SignatureName).HasMaxLength(200);
        builder.Property(x => x.ConsentVersion).HasMaxLength(80);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferenceCode).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.ReferenceCode).IsUnique();
        builder.Property(x => x.ResumeTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);

        // Resume lookups filter by reference code + contact number together; index supports that path.
        builder.HasIndex(x => new { x.ReferenceCode, x.ContactNumber });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.FullNameBn).HasMaxLength(200);
        builder.Property(x => x.FullNameEn).HasMaxLength(200);
        builder.Property(x => x.FatherNameBn).HasMaxLength(200);
        builder.Property(x => x.FatherNameEn).HasMaxLength(200);
        builder.Property(x => x.MotherNameBn).HasMaxLength(200);
        builder.Property(x => x.MotherNameEn).HasMaxLength(200);
        builder.Property(x => x.ContactNumber).HasMaxLength(20);
        builder.Property(x => x.NidNumber).HasMaxLength(30);
        builder.Property(x => x.PermanentAddressBn).HasMaxLength(1000);
        builder.Property(x => x.PermanentAddressEn).HasMaxLength(1000);
        builder.Property(x => x.TemporaryAddressBn).HasMaxLength(1000);
        builder.Property(x => x.TemporaryAddressEn).HasMaxLength(1000);
        builder.Property(x => x.OccupationBn).HasMaxLength(200);
        builder.Property(x => x.OccupationEn).HasMaxLength(200);
        builder.Property(x => x.BloodGroup).HasMaxLength(5);
        builder.Property(x => x.EducationBn).HasMaxLength(200);
        builder.Property(x => x.EducationEn).HasMaxLength(200);
        builder.Property(x => x.EmergencyContactNumber).HasMaxLength(20);
        builder.Property(x => x.PhotoUrl).HasMaxLength(500);
        builder.Property(x => x.PurposeOfJoiningBn).HasMaxLength(2000);
        builder.Property(x => x.PurposeOfJoiningEn).HasMaxLength(2000);
        builder.Property(x => x.LifeGoalBn).HasMaxLength(2000);
        builder.Property(x => x.LifeGoalEn).HasMaxLength(2000);
        builder.Property(x => x.SpecialSkillsBn).HasMaxLength(2000);
        builder.Property(x => x.SpecialSkillsEn).HasMaxLength(2000);
        builder.Property(x => x.CurrentChallengeBn).HasMaxLength(2000);
        builder.Property(x => x.CurrentChallengeEn).HasMaxLength(2000);

        // Allowlisted, structured JSON only — same convention as SecurityAuditEvent.Details.
        builder.Property(x => x.TranslationProvenance).HasMaxLength(4000);
    }
}
