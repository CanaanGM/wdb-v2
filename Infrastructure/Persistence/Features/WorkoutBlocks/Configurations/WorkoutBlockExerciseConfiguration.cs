using Domain.WorkoutBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.WorkoutBlocks.Configurations;

public sealed class WorkoutBlockExerciseConfiguration : IEntityTypeConfiguration<WorkoutBlockExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutBlockExercise> builder)
    {
        builder.ToTable("workout_block_exercise", x =>
        {
            x.HasCheckConstraint("CK_workout_block_exercise_order_non_negative", "order_number >= 0");
            x.HasCheckConstraint("CK_workout_block_exercise_repetitions_non_negative", "repetitions IS NULL OR repetitions >= 0");
            x.HasCheckConstraint("CK_workout_block_exercise_timer_non_negative", "timer_in_seconds IS NULL OR timer_in_seconds >= 0");
            x.HasCheckConstraint("CK_workout_block_exercise_distance_non_negative", "distance_in_meters IS NULL OR distance_in_meters >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.WorkoutBlockId)
            .HasColumnName("workout_block_id")
            .IsRequired();

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id")
            .IsRequired();

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .IsRequired();

        builder.Property(x => x.Instructions)
            .HasColumnName("instructions")
            .HasMaxLength(4000);

        builder.Property(x => x.Repetitions)
            .HasColumnName("repetitions");

        builder.Property(x => x.TimerInSeconds)
            .HasColumnName("timer_in_seconds");

        builder.Property(x => x.DistanceInMeters)
            .HasColumnName("distance_in_meters");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.WorkoutBlockId, x.OrderNumber })
            .HasDatabaseName("IX_workout_block_exercise_workout_block_id_order_number")
            .IsUnique();

        builder.HasIndex(x => x.ExerciseId)
            .HasDatabaseName("IX_workout_block_exercise_exercise_id");

        builder.HasOne(x => x.WorkoutBlock)
            .WithMany(x => x.BlockExercises)
            .HasForeignKey(x => x.WorkoutBlockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Exercise)
            .WithMany()
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
