using Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Exercises.Configurations;

public sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("exercise", x =>
        {
            x.HasCheckConstraint("CK_exercise_difficulty", "difficulty >= 0 AND difficulty <= 5");
            x.HasCheckConstraint("CK_exercise_name_lowercase", "name = lower(name)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.HowTo)
            .HasColumnName("how_to");

        builder.Property(x => x.Difficulty)
            .HasColumnName("difficulty")
            .HasDefaultValue(0);
    }
}
