using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WorkoutLogDatabase")
            ?? throw new InvalidOperationException("Connection string 'WorkoutLogDatabase' was not found.");

        services.AddDbContext<WorkoutLogDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
