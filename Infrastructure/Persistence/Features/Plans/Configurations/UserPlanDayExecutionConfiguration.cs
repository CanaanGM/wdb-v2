using Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Plans.Configurations;

public sealed class UserPlanDayExecutionConfiguration : IEntityTypeConfiguration<UserPlanDayExecution>
{
    public void Configure(EntityTypeBuilder<UserPlanDayExecution> builder)
    {
        builder.ToTable("user_plan_day_execution", x =>
        {
            x.HasCheckConstraint("CK_user_plan_day_execution_status", "status IN ('scheduled', 'completed', 'skipped', 'partial')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EnrollmentId)
            .HasColumnName("enrollment_id")
            .IsRequired();

        builder.Property(x => x.LocalDate)
            .HasColumnName("local_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.LinkedWorkoutSessionId)
            .HasColumnName("linked_workout_session_id");

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(4000);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.EnrollmentId, x.LocalDate })
            .IsUnique()
            .HasDatabaseName("IX_user_plan_day_execution_enrollment_id_local_date");

        builder.HasOne(x => x.Enrollment)
            .WithMany(x => x.DayExecutions)
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LinkedWorkoutSession)
            .WithMany()
            .HasForeignKey(x => x.LinkedWorkoutSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
