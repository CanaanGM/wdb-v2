using Domain.Muscles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Muscles.Configurations;

public sealed class MuscleConfiguration : IEntityTypeConfiguration<Muscle>
{
    public void Configure(EntityTypeBuilder<Muscle> builder)
    {
        builder.ToTable("muscle", x =>
        {
            x.HasCheckConstraint("CK_muscle_name_lowercase", "name = lower(name)");
            x.HasCheckConstraint("CK_muscle_group_lowercase", "muscle_group = lower(muscle_group)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("IX_muscle_name");

        builder.Property(x => x.MuscleGroup)
            .HasColumnName("muscle_group")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Function)
            .HasColumnName("function");

        builder.Property(x => x.WikiPageUrl)
            .HasColumnName("wiki_page_url")
            .HasMaxLength(2000);
    }
}
