using Domain.WorkoutBlocks;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Features.WorkoutBlocks.Configurations;

public sealed class WorkoutBlockConfiguration : IEntityTypeConfiguration<WorkoutBlock>
{
    public void Configure(EntityTypeBuilder<WorkoutBlock> builder)
    {
        builder.ToTable("workout_block", x =>
        {
            x.HasCheckConstraint("CK_workout_block_name_lowercase", "name = lower(name)");
            x.HasCheckConstraint("CK_workout_block_sets_positive", "sets >= 1");
            x.HasCheckConstraint("CK_workout_block_rest_non_negative", "rest_in_seconds >= 0");
            x.HasCheckConstraint("CK_workout_block_order_non_negative", "order_number >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Sets)
            .HasColumnName("sets")
            .IsRequired();

        builder.Property(x => x.RestInSeconds)
            .HasColumnName("rest_in_seconds")
            .IsRequired();

        builder.Property(x => x.Instructions)
            .HasColumnName("instructions")
            .HasMaxLength(4000);

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_workout_block_user_id");

        builder.HasIndex(x => new { x.UserId, x.Name })
            .HasDatabaseName("IX_workout_block_user_id_name");

        builder.HasOne<AuthUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
