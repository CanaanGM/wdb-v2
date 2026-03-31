using Domain.TrainingTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.TrainingTypes.Configurations;

public sealed class TrainingTypeConfiguration : IEntityTypeConfiguration<TrainingType>
{
    public void Configure(EntityTypeBuilder<TrainingType> builder)
    {
        builder.ToTable("training_type", x =>
        {
            x.HasCheckConstraint("CK_training_type_name_lowercase", "name = lower(name)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_training_type_name")
            .IsUnique();
    }
}
