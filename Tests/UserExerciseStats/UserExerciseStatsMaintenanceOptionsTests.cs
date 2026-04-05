using Api.Features.UserExerciseStats;
using Api.Features.UserExerciseStats.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace WorkoutLog.Tests.UserExerciseStats;

public sealed class UserExerciseStatsMaintenanceOptionsTests
{
    [Fact]
    public void BlankOverride_FallsBackToLowerPriorityConfiguredValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{UserExerciseStatsMaintenanceOptions.SectionName}:{nameof(UserExerciseStatsMaintenanceOptions.RecomputeAllOnStartup)}"] = "true"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{UserExerciseStatsMaintenanceOptions.SectionName}:{nameof(UserExerciseStatsMaintenanceOptions.RecomputeAllOnStartup)}"] = string.Empty
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddUserExerciseStatsFeature(configuration);

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<UserExerciseStatsMaintenanceOptions>>().Value;

        Assert.True(options.RecomputeAllOnStartup);
    }

    [Fact]
    public void InvalidNonEmptyValue_StillThrows()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{UserExerciseStatsMaintenanceOptions.SectionName}:{nameof(UserExerciseStatsMaintenanceOptions.RecomputeAllOnStartup)}"] = "not-a-bool"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddUserExerciseStatsFeature(configuration);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IOptions<UserExerciseStatsMaintenanceOptions>>().Value);

        Assert.Contains(UserExerciseStatsMaintenanceOptions.SectionName, exception.Message);
    }
}
