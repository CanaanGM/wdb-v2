using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class WorkoutLogDbContext(DbContextOptions<WorkoutLogDbContext> options) : DbContext(options)
{
    public DbSet<MigrationProbe> MigrationProbes => Set<MigrationProbe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MigrationProbe>(entity =>
        {
            entity.ToTable("migration_probes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasDefaultValueSql("now()");
        });
    }
}
