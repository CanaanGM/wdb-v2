using Api.Features.UserExerciseStats.Services;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;
using Domain.Exercises;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Sdk;

namespace WorkoutLog.Tests.Integration;

public static class IntegrationTestCollections
{
    public const string PostgreSql = "postgresql-integration";
}

[CollectionDefinition(IntegrationTestCollections.PostgreSql)]
public sealed class PostgreSqlIntegrationCollection : ICollectionFixture<PostgreSqlFixture>
{
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    private Exception? _startException;

    public bool IsAvailable => _startException is null && _container is not null;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("workoutlog_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
            await ResetDatabaseAsync();
        }
        catch (Exception ex)
        {
            _startException = ex;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        EnsureAvailable();

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public WorkoutLogDbContext CreateContext()
    {
        EnsureAvailable();

        var options = new DbContextOptionsBuilder<WorkoutLogDbContext>()
            .UseNpgsql(_container!.GetConnectionString())
            .Options;

        return new WorkoutLogDbContext(options);
    }

    private void EnsureAvailable()
    {
        if (_startException is null)
        {
            if (_container is not null)
            {
                return;
            }

            throw SkipException.ForSkip("PostgreSQL testcontainer is unavailable: fixture was not initialized.");
        }

        throw SkipException.ForSkip($"PostgreSQL testcontainer is unavailable: {_startException.Message}");
    }

}

[Collection(IntegrationTestCollections.PostgreSql)]
public sealed class PostgreSqlWorkoutIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CreateAndSearchAsync_PreservesFeelingCasing_AndUsesPostgresIlike()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var (workoutsService, _) = CreateServices(context);

        var create = await workoutsService.CreateAsync(
            1,
            new CreateWorkoutRequest
            {
                Feeling = "  Felt Amazing After PR  ",
                DurationInMinutes = 45,
                Mood = 9,
                Entries =
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, repetitions: 10, weightUsedKg: 100, kcalBurned: 200)
                ]
            },
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, create.ResultType);
        Assert.NotNull(create.Value);
        Assert.Equal("Felt Amazing After PR", create.Value.Feeling);

        var page = await workoutsService.SearchAsync(
            1,
            new SearchWorkoutsRequest
            {
                Search = "amazing",
                PageNumber = 1,
                PageSize = 20
            },
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal("Felt Amazing After PR", page.Items[0].Feeling);
    }

    [Fact]
    public async Task RecomputeAllAsync_RebuildsStatsWithDeterministicLatestWeight_OnPostgres()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var (workoutsService, statsService) = CreateServices(context);

        var firstWorkout = await workoutsService.CreateAsync(
            1,
            new CreateWorkoutRequest
            {
                Feeling = "session-1",
                DurationInMinutes = 30,
                Mood = 7,
                PerformedAtUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                Entries =
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, repetitions: 5, weightUsedKg: 40, kcalBurned: 100),
                    BuildEntry(exerciseId: 1, orderNumber: 2, repetitions: 5, weightUsedKg: 45, kcalBurned: 100)
                ]
            },
            CancellationToken.None);

        var secondWorkout = await workoutsService.CreateAsync(
            1,
            new CreateWorkoutRequest
            {
                Feeling = "session-2",
                DurationInMinutes = 30,
                Mood = 7,
                PerformedAtUtc = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                Entries =
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, repetitions: 5, weightUsedKg: 60, kcalBurned: 100),
                    BuildEntry(exerciseId: 1, orderNumber: 2, repetitions: 5, weightUsedKg: 70, kcalBurned: 100)
                ]
            },
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, firstWorkout.ResultType);
        Assert.Equal(WorkoutOperationResultType.Success, secondWorkout.ResultType);

        var stored = await context.UserExerciseStats.SingleAsync(x => x.UserId == 1 && x.ExerciseId == 1);
        stored.LastUsedWeightKg = 1;
        stored.BestWeightKg = 1;
        stored.AverageWeightKg = 1;
        await context.SaveChangesAsync();

        await statsService.RecomputeAllAsync(CancellationToken.None);

        var stat = await statsService.GetByExerciseIdAsync(1, 1, CancellationToken.None);
        Assert.NotNull(stat);
        Assert.Equal(4, stat.UseCount);
        Assert.Equal(70d, stat.BestWeightKg, 6);
        Assert.Equal(53.75d, stat.AverageWeightKg, 6);
        Assert.Equal(70d, stat.LastUsedWeightKg, 6);
        Assert.Equal(new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc), stat.LastPerformedAtUtc);
    }

    private static (WorkoutsService WorkoutsService, UserExerciseStatsService UserExerciseStatsService) CreateServices(
        WorkoutLogDbContext context)
    {
        var statsService = new UserExerciseStatsService(context);
        var workoutsService = new WorkoutsService(context, statsService);
        return (workoutsService, statsService);
    }

    private static async Task SeedUsersAndExercisesAsync(
        WorkoutLogDbContext context,
        IReadOnlyCollection<int> userIds,
        IReadOnlyCollection<int> exerciseIds)
    {
        foreach (var userId in userIds)
        {
            context.Users.Add(new AuthUser
            {
                Id = userId,
                UserName = $"user{userId}",
                NormalizedUserName = $"USER{userId}",
                Email = $"user{userId}@example.com",
                NormalizedEmail = $"USER{userId}@EXAMPLE.COM"
            });
        }

        foreach (var exerciseId in exerciseIds)
        {
            context.Exercises.Add(new Exercise
            {
                Id = exerciseId,
                Name = $"exercise-{exerciseId}",
                Difficulty = 1
            });
        }

        await context.SaveChangesAsync();
    }

    private static WorkoutEntryRequest BuildEntry(
        int exerciseId,
        int orderNumber,
        int repetitions,
        double weightUsedKg,
        int kcalBurned)
    {
        return new WorkoutEntryRequest
        {
            ExerciseId = exerciseId,
            OrderNumber = orderNumber,
            Repetitions = repetitions,
            Mood = 5,
            WeightUsedKg = weightUsedKg,
            KcalBurned = kcalBurned,
            RateOfPerceivedExertion = 6
        };
    }
}
