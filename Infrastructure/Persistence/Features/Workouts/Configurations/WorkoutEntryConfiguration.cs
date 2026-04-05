using Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Workouts.Configurations;

public sealed class WorkoutEntryConfiguration : IEntityTypeConfiguration<WorkoutEntry>
{
    public void Configure(EntityTypeBuilder<WorkoutEntry> builder)
    {
        builder.ToTable("workout_entry", x =>
        {
            x.HasCheckConstraint("CK_workout_entry_order_number_non_negative", "order_number >= 0");
            x.HasCheckConstraint("CK_workout_entry_repetitions_non_negative", "repetitions >= 0");
            x.HasCheckConstraint("CK_workout_entry_mood_range", "mood >= 0 AND mood <= 10");
            x.HasCheckConstraint("CK_workout_entry_timer_non_negative", "timer_in_seconds IS NULL OR timer_in_seconds >= 0");
            x.HasCheckConstraint("CK_workout_entry_weight_non_negative", "weight_used_kg >= 0");
            x.HasCheckConstraint("CK_workout_entry_rpe_range", "rate_of_perceived_exertion >= 0 AND rate_of_perceived_exertion <= 10");
            x.HasCheckConstraint("CK_workout_entry_rest_non_negative", "rest_in_seconds IS NULL OR rest_in_seconds >= 0");
            x.HasCheckConstraint("CK_workout_entry_kcal_non_negative", "kcal_burned >= 0");
            x.HasCheckConstraint("CK_workout_entry_distance_non_negative", "distance_in_meters IS NULL OR distance_in_meters >= 0");
            x.HasCheckConstraint("CK_workout_entry_incline_non_negative", "incline IS NULL OR incline >= 0");
            x.HasCheckConstraint("CK_workout_entry_speed_non_negative", "speed IS NULL OR speed >= 0");
            x.HasCheckConstraint("CK_workout_entry_heart_rate_non_negative", "heart_rate_avg IS NULL OR heart_rate_avg >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.WorkoutSessionId)
            .HasColumnName("workout_session_id")
            .IsRequired();

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id")
            .IsRequired();

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .IsRequired();

        builder.Property(x => x.Repetitions)
            .HasColumnName("repetitions")
            .IsRequired();

        builder.Property(x => x.Mood)
            .HasColumnName("mood")
            .IsRequired();

        builder.Property(x => x.TimerInSeconds)
            .HasColumnName("timer_in_seconds");

        builder.Property(x => x.WeightUsedKg)
            .HasColumnName("weight_used_kg")
            .IsRequired();

        builder.Property(x => x.RateOfPerceivedExertion)
            .HasColumnName("rate_of_perceived_exertion")
            .IsRequired();

        builder.Property(x => x.RestInSeconds)
            .HasColumnName("rest_in_seconds");

        builder.Property(x => x.KcalBurned)
            .HasColumnName("kcal_burned")
            .IsRequired();

        builder.Property(x => x.DistanceInMeters)
            .HasColumnName("distance_in_meters");

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(4000);

        builder.Property(x => x.Incline)
            .HasColumnName("incline");

        builder.Property(x => x.Speed)
            .HasColumnName("speed");

        builder.Property(x => x.HeartRateAvg)
            .HasColumnName("heart_rate_avg");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.WorkoutSessionId, x.OrderNumber })
            .HasDatabaseName("IX_workout_entry_workout_session_id_order_number")
            .IsUnique();

        builder.HasIndex(x => x.ExerciseId)
            .HasDatabaseName("IX_workout_entry_exercise_id");

        builder.HasOne(x => x.WorkoutSession)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Exercise)
            .WithMany()
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
