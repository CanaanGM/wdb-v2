using Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Plans.Configurations;

public sealed class UserPlanExerciseExecutionConfiguration : IEntityTypeConfiguration<UserPlanExerciseExecution>
{
    public void Configure(EntityTypeBuilder<UserPlanExerciseExecution> builder)
    {
        builder.ToTable("user_plan_exercise_execution", x =>
        {
            x.HasCheckConstraint("CK_user_plan_exercise_execution_status", "status IN ('pending', 'completed', 'skipped')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.DayExecutionId)
            .HasColumnName("day_execution_id")
            .IsRequired();

        builder.Property(x => x.PlanDayExerciseId)
            .HasColumnName("plan_day_exercise_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.LinkedWorkoutEntryId)
            .HasColumnName("linked_workout_entry_id");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.DayExecutionId, x.PlanDayExerciseId })
            .IsUnique()
            .HasDatabaseName("IX_user_plan_exercise_execution_day_execution_id_plan_day_exercise_id");

        builder.HasOne(x => x.DayExecution)
            .WithMany(x => x.ExerciseExecutions)
            .HasForeignKey(x => x.DayExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PlanDayExercise)
            .WithMany()
            .HasForeignKey(x => x.PlanDayExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LinkedWorkoutEntry)
            .WithMany()
            .HasForeignKey(x => x.LinkedWorkoutEntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
