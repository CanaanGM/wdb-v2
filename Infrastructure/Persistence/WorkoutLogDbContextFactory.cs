using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

public sealed class WorkoutLogDbContextFactory : IDesignTimeDbContextFactory<WorkoutLogDbContext>
{
    public WorkoutLogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("WORKOUTLOG_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=workoutlog;Username=workoutlog;Password=workoutlog";

        var optionsBuilder = new DbContextOptionsBuilder<WorkoutLogDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WorkoutLogDbContext(optionsBuilder.Options);
    }
}
