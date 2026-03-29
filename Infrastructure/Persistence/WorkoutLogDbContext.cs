using Domain.Equipments;
using Domain.Exercises;
using Domain.Muscles;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class WorkoutLogDbContext(DbContextOptions<WorkoutLogDbContext> options) : DbContext(options)
{
    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<ExerciseHowTo> ExerciseHowTos => Set<ExerciseHowTo>();

    public DbSet<ExerciseMuscle> ExerciseMuscles => Set<ExerciseMuscle>();

    public DbSet<ExerciseEquipment> ExerciseEquipments => Set<ExerciseEquipment>();

    public DbSet<Muscle> Muscles => Set<Muscle>();

    public DbSet<Equipment> Equipments => Set<Equipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkoutLogDbContext).Assembly);
    }
}
