using Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Plans.Configurations;

public sealed class PlanTemplateConfiguration : IEntityTypeConfiguration<PlanTemplate>
{
    public void Configure(EntityTypeBuilder<PlanTemplate> builder)
    {
        builder.ToTable("plan_template", x =>
        {
            x.HasCheckConstraint("CK_plan_template_slug_lowercase", "slug = lower(slug)");
            x.HasCheckConstraint("CK_plan_template_duration_weeks_positive", "duration_weeks >= 1");
            x.HasCheckConstraint("CK_plan_template_version_positive", "version >= 1");
            x.HasCheckConstraint("CK_plan_template_status", "status IN ('draft', 'published', 'archived')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(4000);

        builder.Property(x => x.DurationWeeks)
            .HasColumnName("duration_weeks")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.Slug, x.Version })
            .IsUnique()
            .HasDatabaseName("IX_plan_template_slug_version");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_plan_template_status");
    }
}
