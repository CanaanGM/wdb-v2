using Domain.Workouts;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.Workouts.Configurations;

public sealed class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("workout_session", x =>
        {
            x.HasCheckConstraint("CK_workout_session_duration_non_negative", "duration_in_seconds >= 0");
            x.HasCheckConstraint("CK_workout_session_calories_non_negative", "calories >= 0");
            x.HasCheckConstraint("CK_workout_session_total_kg_non_negative", "total_kg_moved >= 0");
            x.HasCheckConstraint("CK_workout_session_total_repetitions_non_negative", "total_repetitions >= 0");
            x.HasCheckConstraint("CK_workout_session_avg_rpe_range", "average_rate_of_perceived_exertion >= 0 AND average_rate_of_perceived_exertion <= 10");
            x.HasCheckConstraint("CK_workout_session_mood_range", "mood >= 0 AND mood <= 10");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Mood)
            .HasColumnName("mood")
            .IsRequired();

        builder.Property(x => x.Feeling)
            .HasColumnName("feeling")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(4000);

        builder.Property(x => x.DurationInSeconds)
            .HasColumnName("duration_in_seconds")
            .IsRequired();

        builder.Property(x => x.Calories)
            .HasColumnName("calories")
            .IsRequired();

        builder.Property(x => x.TotalKgMoved)
            .HasColumnName("total_kg_moved")
            .IsRequired();

        builder.Property(x => x.TotalRepetitions)
            .HasColumnName("total_repetitions")
            .IsRequired();

        builder.Property(x => x.AverageRateOfPerceivedExertion)
            .HasColumnName("average_rate_of_perceived_exertion")
            .IsRequired();

        builder.Property(x => x.PerformedAtUtc)
            .HasColumnName("performed_at_utc")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PerformedAtUtc })
            .HasDatabaseName("IX_workout_session_user_id_performed_at_utc");

        builder.HasOne<AuthUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
