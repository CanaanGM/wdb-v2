using Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Exercises.Configurations;

public sealed class ExerciseMuscleConfiguration : IEntityTypeConfiguration<ExerciseMuscle>
{
    public void Configure(EntityTypeBuilder<ExerciseMuscle> builder)
    {
        builder.ToTable("exercise_muscle");

        builder.HasKey(x => new { x.MuscleId, x.ExerciseId });

        builder.Property(x => x.MuscleId)
            .HasColumnName("muscle_id");

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id");

        builder.Property(x => x.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false);

        builder.HasIndex(x => x.IsPrimary)
            .HasDatabaseName("idx_exercise_muscle_is_primary");

        builder.HasOne(x => x.Exercise)
            .WithMany(x => x.ExerciseMuscles)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Muscle)
            .WithMany(x => x.ExerciseMuscles)
            .HasForeignKey(x => x.MuscleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
