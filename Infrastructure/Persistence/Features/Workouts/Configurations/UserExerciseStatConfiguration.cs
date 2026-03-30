using Domain.Workouts;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Workouts.Configurations;

public sealed class UserExerciseStatConfiguration : IEntityTypeConfiguration<UserExerciseStat>
{
    public void Configure(EntityTypeBuilder<UserExerciseStat> builder)
    {
        builder.ToTable("user_exercise_stat", x =>
        {
            x.HasCheckConstraint("CK_user_exercise_stat_use_count_non_negative", "use_count >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_best_weight_non_negative", "best_weight_kg >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_average_weight_non_negative", "average_weight_kg >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_last_weight_non_negative", "last_used_weight_kg >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_avg_timer_non_negative", "average_timer_in_seconds IS NULL OR average_timer_in_seconds >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_avg_heart_rate_non_negative", "average_heart_rate IS NULL OR average_heart_rate >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_avg_kcal_non_negative", "average_kcal_burned IS NULL OR average_kcal_burned >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_avg_distance_non_negative", "average_distance_meters IS NULL OR average_distance_meters >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_avg_speed_non_negative", "average_speed IS NULL OR average_speed >= 0");
            x.HasCheckConstraint("CK_user_exercise_stat_avg_rpe_range", "average_rate_of_perceived_exertion IS NULL OR (average_rate_of_perceived_exertion >= 0 AND average_rate_of_perceived_exertion <= 10)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id")
            .IsRequired();

        builder.Property(x => x.UseCount)
            .HasColumnName("use_count")
            .IsRequired();

        builder.Property(x => x.BestWeightKg)
            .HasColumnName("best_weight_kg")
            .IsRequired();

        builder.Property(x => x.AverageWeightKg)
            .HasColumnName("average_weight_kg")
            .IsRequired();

        builder.Property(x => x.LastUsedWeightKg)
            .HasColumnName("last_used_weight_kg")
            .IsRequired();

        builder.Property(x => x.AverageTimerInSeconds)
            .HasColumnName("average_timer_in_seconds");

        builder.Property(x => x.AverageHeartRate)
            .HasColumnName("average_heart_rate");

        builder.Property(x => x.AverageKcalBurned)
            .HasColumnName("average_kcal_burned");

        builder.Property(x => x.AverageDistanceMeters)
            .HasColumnName("average_distance_meters");

        builder.Property(x => x.AverageSpeed)
            .HasColumnName("average_speed");

        builder.Property(x => x.AverageRateOfPerceivedExertion)
            .HasColumnName("average_rate_of_perceived_exertion");

        builder.Property(x => x.LastPerformedAtUtc)
            .HasColumnName("last_performed_at_utc")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ExerciseId })
            .HasDatabaseName("IX_user_exercise_stat_user_id_exercise_id")
            .IsUnique();

        builder.HasOne<AuthUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Exercise)
            .WithMany()
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
