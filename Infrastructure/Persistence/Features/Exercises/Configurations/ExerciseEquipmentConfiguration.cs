using Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Exercises.Configurations;

public sealed class ExerciseEquipmentConfiguration : IEntityTypeConfiguration<ExerciseEquipment>
{
    public void Configure(EntityTypeBuilder<ExerciseEquipment> builder)
    {
        builder.ToTable("exercise_equipment");

        builder.HasKey(x => new { x.EquipmentId, x.ExerciseId });

        builder.Property(x => x.EquipmentId)
            .HasColumnName("equipment_id");

        builder.Property(x => x.ExerciseId)
            .HasColumnName("exercise_id");

        builder.HasOne(x => x.Exercise)
            .WithMany(x => x.ExerciseEquipments)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Equipment)
            .WithMany(x => x.ExerciseEquipments)
            .HasForeignKey(x => x.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
