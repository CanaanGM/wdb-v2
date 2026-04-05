using Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Exercises.Configurations;

public sealed class ExerciseTrainingTypeConfiguration : IEntityTypeConfiguration<ExerciseTrainingType>
{
    public void Configure(EntityTypeBuilder<ExerciseTrainingType> builder)
    {
        builder.ToTable("exercise_training_type");

        builder.HasKey(x => new { x.TrainingTypeId, x.ExerciseId });

        builder.Property(x => x.TrainingTypeId)
            .HasColumnName("training_type_id");

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id");

        builder.HasOne(x => x.Exercise)
            .WithMany(x => x.ExerciseTrainingTypes)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TrainingType)
            .WithMany(x => x.ExerciseTrainingTypes)
            .HasForeignKey(x => x.TrainingTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
