using Api.Features.UserExerciseStats.Options;
using Api.Features.UserExerciseStats.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Features.UserExerciseStats;

public sealed class UserExerciseStatsMaintenanceHostedService(
    IServiceProvider serviceProvider,
    IOptions<UserExerciseStatsMaintenanceOptions> maintenanceOptions,
    ILogger<UserExerciseStatsMaintenanceHostedService> logger) : IHostedService
{
    private readonly UserExerciseStatsMaintenanceOptions _maintenanceOptions = maintenanceOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_maintenanceOptions.RecomputeAllOnStartup)
        {
            return;
        }

        var startedAtUtc = DateTime.UtcNow;
        logger.LogInformation("User exercise stats full recompute on startup is enabled. Starting maintenance pass.");

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutLogDbContext>();
            var userExerciseStatsService = scope.ServiceProvider.GetRequiredService<IUserExerciseStatsService>();

            var statsBefore = await dbContext.UserExerciseStats.CountAsync(cancellationToken);
            var distinctSourcePairs = await dbContext.WorkoutEntries
                .AsNoTracking()
                .Select(x => new
                {
                    x.WorkoutSession.UserId,
                    x.ExerciseId
                })
                .Distinct()
                .CountAsync(cancellationToken);

            await userExerciseStatsService.RecomputeAllAsync(cancellationToken);

            var statsAfter = await dbContext.UserExerciseStats.CountAsync(cancellationToken);
            var elapsedMs = (DateTime.UtcNow - startedAtUtc).TotalMilliseconds;

            logger.LogInformation(
                "User exercise stats full recompute completed. Distinct source pairs: {DistinctSourcePairs}, stats before: {StatsBefore}, stats after: {StatsAfter}, elapsedMs: {ElapsedMs}.",
                distinctSourcePairs,
                statsBefore,
                statsAfter,
                elapsedMs);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "User exercise stats full recompute failed during startup.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
