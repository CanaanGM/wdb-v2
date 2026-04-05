using Domain.Plans;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Plans.Configurations;

public sealed class UserPlanEnrollmentConfiguration : IEntityTypeConfiguration<UserPlanEnrollment>
{
    public void Configure(EntityTypeBuilder<UserPlanEnrollment> builder)
    {
        builder.ToTable("user_plan_enrollment", x =>
        {
            x.HasCheckConstraint("CK_user_plan_enrollment_display_order_non_negative", "display_order >= 0");
            x.HasCheckConstraint("CK_user_plan_enrollment_status", "status IN ('active', 'completed', 'cancelled')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.PlanTemplateId)
            .HasColumnName("plan_template_id")
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();

        builder.Property(x => x.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StartLocalDate)
            .HasColumnName("start_local_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.EndLocalDateInclusive)
            .HasColumnName("end_local_date_inclusive")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("IX_user_plan_enrollment_user_id_status");

        builder.HasIndex(x => new { x.UserId, x.PlanTemplateId, x.Status })
            .HasDatabaseName("IX_user_plan_enrollment_user_id_plan_template_id_status");

        builder.HasOne<AuthUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PlanTemplate)
            .WithMany()
            .HasForeignKey(x => x.PlanTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
