using Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Plans.Configurations;

public sealed class PlanDayExerciseConfiguration : IEntityTypeConfiguration<PlanDayExercise>
{
    public void Configure(EntityTypeBuilder<PlanDayExercise> builder)
    {
        builder.ToTable("plan_day_exercise", x =>
        {
            x.HasCheckConstraint("CK_plan_day_exercise_order_non_negative", "order_number >= 0");
            x.HasCheckConstraint("CK_plan_day_exercise_sets_non_negative", "sets IS NULL OR sets >= 0");
            x.HasCheckConstraint("CK_plan_day_exercise_repetitions_non_negative", "repetitions IS NULL OR repetitions >= 0");
            x.HasCheckConstraint("CK_plan_day_exercise_target_rpe_range", "target_rate_of_perceived_exertion IS NULL OR (target_rate_of_perceived_exertion >= 0 AND target_rate_of_perceived_exertion <= 10)");
            x.HasCheckConstraint("CK_plan_day_exercise_target_weight_non_negative", "target_weight_kg IS NULL OR target_weight_kg >= 0");
            x.HasCheckConstraint("CK_plan_day_exercise_timer_non_negative", "timer_in_seconds IS NULL OR timer_in_seconds >= 0");
            x.HasCheckConstraint("CK_plan_day_exercise_distance_non_negative", "distance_in_meters IS NULL OR distance_in_meters >= 0");
            x.HasCheckConstraint("CK_plan_day_exercise_rest_non_negative", "rest_in_seconds IS NULL OR rest_in_seconds >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.PlanDayId)
            .HasColumnName("plan_day_id")
            .IsRequired();

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id")
            .IsRequired();

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .IsRequired();

        builder.Property(x => x.Sets)
            .HasColumnName("sets");

        builder.Property(x => x.Repetitions)
            .HasColumnName("repetitions");

        builder.Property(x => x.TargetRateOfPerceivedExertion)
            .HasColumnName("target_rate_of_perceived_exertion");

        builder.Property(x => x.TargetWeightKg)
            .HasColumnName("target_weight_kg");

        builder.Property(x => x.TimerInSeconds)
            .HasColumnName("timer_in_seconds");

        builder.Property(x => x.DistanceInMeters)
            .HasColumnName("distance_in_meters");

        builder.Property(x => x.RestInSeconds)
            .HasColumnName("rest_in_seconds");

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(4000);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.PlanDayId, x.OrderNumber })
            .IsUnique()
            .HasDatabaseName("IX_plan_day_exercise_plan_day_id_order_number");

        builder.HasIndex(x => x.ExerciseId)
            .HasDatabaseName("IX_plan_day_exercise_exercise_id");

        builder.HasOne(x => x.PlanDay)
            .WithMany(x => x.Exercises)
            .HasForeignKey(x => x.PlanDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Exercise)
            .WithMany()
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
