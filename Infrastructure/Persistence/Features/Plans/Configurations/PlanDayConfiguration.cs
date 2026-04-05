using Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Plans.Configurations;

public sealed class PlanDayConfiguration : IEntityTypeConfiguration<PlanDay>
{
    public void Configure(EntityTypeBuilder<PlanDay> builder)
    {
        builder.ToTable("plan_day", x =>
        {
            x.HasCheckConstraint("CK_plan_day_week_positive", "week_number >= 1");
            x.HasCheckConstraint("CK_plan_day_day_range", "day_number >= 1 AND day_number <= 7");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.PlanTemplateId)
            .HasColumnName("plan_template_id")
            .IsRequired();

        builder.Property(x => x.WeekNumber)
            .HasColumnName("week_number")
            .IsRequired();

        builder.Property(x => x.DayNumber)
            .HasColumnName("day_number")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(255);

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(4000);

        builder.HasIndex(x => new { x.PlanTemplateId, x.WeekNumber, x.DayNumber })
            .IsUnique()
            .HasDatabaseName("IX_plan_day_plan_template_id_week_number_day_number");

        builder.HasOne(x => x.PlanTemplate)
            .WithMany(x => x.Days)
            .HasForeignKey(x => x.PlanTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
