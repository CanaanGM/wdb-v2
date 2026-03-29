using Domain.Exercises;
using Domain.Muscles;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class WorkoutLogDbContext(DbContextOptions<WorkoutLogDbContext> options) : DbContext(options)
{
    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<ExerciseHowTo> ExerciseHowTos => Set<ExerciseHowTo>();

    public DbSet<ExerciseMuscle> ExerciseMuscles => Set<ExerciseMuscle>();

    public DbSet<Muscle> Muscles => Set<Muscle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkoutLogDbContext).Assembly);
    }
}
