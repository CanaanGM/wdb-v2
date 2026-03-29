using Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Exercises.Configurations;

public sealed class ExerciseHowToConfiguration : IEntityTypeConfiguration<ExerciseHowTo>
{
    public void Configure(EntityTypeBuilder<ExerciseHowTo> builder)
    {
        builder.ToTable("exercise_how_to");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasOne(x => x.Exercise)
            .WithMany(x => x.HowTos)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
