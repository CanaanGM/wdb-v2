using Domain.Measurements;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Measurements.Configurations;

public sealed class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
{
    public void Configure(EntityTypeBuilder<Measurement> builder)
    {
        builder.ToTable("measurement", x =>
        {
            x.HasCheckConstraint("CK_measurement_user_id_positive", "user_id > 0");
            x.HasCheckConstraint("CK_measurement_bmr_non_negative", "basal_metabolic_rate IS NULL OR basal_metabolic_rate >= 0");
            x.HasCheckConstraint("CK_measurement_visceral_fat_non_negative", "visceral_fat_level IS NULL OR visceral_fat_level >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Hip).HasColumnName("hip");
        builder.Property(x => x.Chest).HasColumnName("chest");
        builder.Property(x => x.WaistUnderBelly).HasColumnName("waist_under_belly");
        builder.Property(x => x.WaistOnBelly).HasColumnName("waist_on_belly");
        builder.Property(x => x.LeftThigh).HasColumnName("left_thigh");
        builder.Property(x => x.RightThigh).HasColumnName("right_thigh");
        builder.Property(x => x.LeftCalf).HasColumnName("left_calf");
        builder.Property(x => x.RightCalf).HasColumnName("right_calf");
        builder.Property(x => x.LeftUpperArm).HasColumnName("left_upper_arm");
        builder.Property(x => x.LeftForearm).HasColumnName("left_forearm");
        builder.Property(x => x.RightUpperArm).HasColumnName("right_upper_arm");
        builder.Property(x => x.RightForearm).HasColumnName("right_forearm");
        builder.Property(x => x.Neck).HasColumnName("neck");
        builder.Property(x => x.Minerals).HasColumnName("minerals");
        builder.Property(x => x.Protein).HasColumnName("protein");
        builder.Property(x => x.TotalBodyWater).HasColumnName("total_body_water");
        builder.Property(x => x.BodyFatMass).HasColumnName("body_fat_mass");
        builder.Property(x => x.BodyWeight).HasColumnName("body_weight");
        builder.Property(x => x.BodyFatPercentage).HasColumnName("body_fat_percentage");
        builder.Property(x => x.SkeletalMuscleMass).HasColumnName("skeletal_muscle_mass");
        builder.Property(x => x.InBodyScore).HasColumnName("in_body_score");
        builder.Property(x => x.BodyMassIndex).HasColumnName("body_mass_index");
        builder.Property(x => x.BasalMetabolicRate).HasColumnName("basal_metabolic_rate");
        builder.Property(x => x.VisceralFatLevel).HasColumnName("visceral_fat_level");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc })
            .HasDatabaseName("IX_measurement_user_id_created_at_utc");

        builder.HasOne<AuthUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
