using Domain.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Equipments.Configurations;

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment", x =>
        {
            x.HasCheckConstraint("CK_equipment_name_lowercase", "name = lower(name)");
            x.HasCheckConstraint("CK_equipment_weight_non_negative", "weight_kg >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_equipment_name")
            .IsUnique();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.HowTo)
            .HasColumnName("how_to");

        builder.Property(x => x.WeightKg)
            .HasColumnName("weight_kg")
            .HasDefaultValue(0d);

        builder.HasIndex(x => x.WeightKg)
            .HasDatabaseName("idx_equipment_weight");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()");
    }
}
