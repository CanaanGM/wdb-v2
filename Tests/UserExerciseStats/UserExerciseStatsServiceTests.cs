using Api.Features.UserExerciseStats.Services;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;
using Domain.Exercises;
using Domain.Workouts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.UserExerciseStats;

public sealed class UserExerciseStatsServiceTests
{
    [Fact]
    public async Task CreateAsync_RepeatedExerciseEntries_UsesDeterministicLastUsedWeight()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var (workoutsService, statsService) = CreateServices(context);

        var createFirst = await workoutsService.CreateAsync(
            1,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 40, kcalBurned: 100),
                    BuildEntry(exerciseId: 1, orderNumber: 2, weightUsedKg: 60, kcalBurned: 100)
                ]),
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, createFirst.ResultType);

        var createSecond = await workoutsService.CreateAsync(
            1,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 80, kcalBurned: 100),
                    BuildEntry(exerciseId: 1, orderNumber: 2, weightUsedKg: 90, kcalBurned: 100),
                    BuildEntry(exerciseId: 1, orderNumber: 3, weightUsedKg: 95, kcalBurned: 100)
                ]),
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, createSecond.ResultType);

        var stat = await statsService.GetByExerciseIdAsync(1, 1, CancellationToken.None);

        Assert.NotNull(stat);
        Assert.Equal(5, stat.UseCount);
        Assert.Equal(95d, stat.BestWeightKg, 6);
        Assert.Equal(73d, stat.AverageWeightKg, 6);
        Assert.Equal(95d, stat.LastUsedWeightKg, 6);
        Assert.Equal(new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc), stat.LastPerformedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_RemovedExercise_DropsStaleStatsAndRecomputesRemaining()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2]);
        var (workoutsService, statsService) = CreateServices(context);

        var created = await workoutsService.CreateAsync(
            1,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 3, 9, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 10, kcalBurned: 10),
                    BuildEntry(exerciseId: 2, orderNumber: 2, weightUsedKg: 20, kcalBurned: 10)
                ]),
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, created.ResultType);
        Assert.NotNull(created.Value);

        var updated = await workoutsService.UpdateAsync(
            1,
            created.Value.Id,
            new UpdateWorkoutRequest
            {
                Feeling = "updated",
                DurationInMinutes = 30,
                Mood = 6,
                PerformedAtUtc = new DateTime(2026, 1, 4, 9, 0, 0, DateTimeKind.Utc),
                Entries =
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 30, kcalBurned: 20),
                    BuildEntry(exerciseId: 1, orderNumber: 2, weightUsedKg: 40, kcalBurned: 20)
                ]
            },
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, updated.ResultType);

        var exercise1Stat = await statsService.GetByExerciseIdAsync(1, 1, CancellationToken.None);
        var exercise2Stat = await statsService.GetByExerciseIdAsync(1, 2, CancellationToken.None);

        Assert.NotNull(exercise1Stat);
        Assert.Equal(2, exercise1Stat.UseCount);
        Assert.Equal(40d, exercise1Stat.BestWeightKg, 6);
        Assert.Equal(35d, exercise1Stat.AverageWeightKg, 6);
        Assert.Equal(40d, exercise1Stat.LastUsedWeightKg, 6);
        Assert.Null(exercise2Stat);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOrUpdatesStatsForAffectedExercises()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2, 3]);
        var (workoutsService, statsService) = CreateServices(context);

        var firstWorkout = await workoutsService.CreateAsync(
            1,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 5, 7, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 10, kcalBurned: 10),
                    BuildEntry(exerciseId: 2, orderNumber: 2, weightUsedKg: 50, kcalBurned: 10)
                ]),
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, firstWorkout.ResultType);
        Assert.NotNull(firstWorkout.Value);

        var secondWorkout = await workoutsService.CreateAsync(
            1,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 6, 7, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 30, kcalBurned: 10),
                    BuildEntry(exerciseId: 3, orderNumber: 2, weightUsedKg: 70, kcalBurned: 10)
                ]),
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, secondWorkout.ResultType);
        Assert.NotNull(secondWorkout.Value);

        var deleted = await workoutsService.DeleteAsync(1, secondWorkout.Value.Id, CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, deleted.ResultType);

        var exercise1Stat = await statsService.GetByExerciseIdAsync(1, 1, CancellationToken.None);
        var exercise2Stat = await statsService.GetByExerciseIdAsync(1, 2, CancellationToken.None);
        var exercise3Stat = await statsService.GetByExerciseIdAsync(1, 3, CancellationToken.None);

        Assert.NotNull(exercise1Stat);
        Assert.Equal(1, exercise1Stat.UseCount);
        Assert.Equal(10d, exercise1Stat.LastUsedWeightKg, 6);

        Assert.NotNull(exercise2Stat);
        Assert.Equal(1, exercise2Stat.UseCount);

        Assert.Null(exercise3Stat);
    }

    [Fact]
    public async Task RecomputeForExercisesAsync_OptionalCardioMetricsStayNull_WhenNotRecorded()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var (workoutsService, statsService) = CreateServices(context);

        var created = await workoutsService.CreateAsync(
            1,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 7, 8, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 25, kcalBurned: 15),
                    BuildEntry(exerciseId: 1, orderNumber: 2, weightUsedKg: 35, kcalBurned: 25)
                ]),
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, created.ResultType);

        var stat = await statsService.GetByExerciseIdAsync(1, 1, CancellationToken.None);

        Assert.NotNull(stat);
        Assert.Null(stat.AverageTimerInSeconds);
        Assert.Null(stat.AverageHeartRate);
        Assert.Null(stat.AverageDistanceMeters);
        Assert.Null(stat.AverageSpeed);
    }

    [Fact]
    public async Task RecomputeAllAsync_BackfillsIncorrectRows_AndRemovesStaleRows()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1, 2], [1, 2, 3]);
        var (workoutsService, statsService) = CreateServices(context);

        var user1Create = await workoutsService.CreateAsync(
            1,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 8, 6, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 20, kcalBurned: 10),
                    BuildEntry(exerciseId: 2, orderNumber: 2, weightUsedKg: 70, kcalBurned: 20, timerInSeconds: 300)
                ]),
            CancellationToken.None);

        var user2Create = await workoutsService.CreateAsync(
            2,
            BuildWorkoutRequest(
                performedAtUtc: new DateTime(2026, 1, 9, 6, 0, 0, DateTimeKind.Utc),
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, weightUsedKg: 50, kcalBurned: 10)
                ]),
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, user1Create.ResultType);
        Assert.Equal(WorkoutOperationResultType.Success, user2Create.ResultType);

        var user1Exercise1 = await context.UserExerciseStats.SingleAsync(x => x.UserId == 1 && x.ExerciseId == 1);
        user1Exercise1.UseCount = 999;
        user1Exercise1.BestWeightKg = 999;
        user1Exercise1.AverageWeightKg = 999;
        user1Exercise1.LastUsedWeightKg = 999;

        var user2Exercise1 = await context.UserExerciseStats.SingleAsync(x => x.UserId == 2 && x.ExerciseId == 1);
        user2Exercise1.BestWeightKg = 999;
        user2Exercise1.LastUsedWeightKg = 999;

        context.UserExerciseStats.Add(new UserExerciseStat
        {
            UserId = 1,
            ExerciseId = 3,
            UseCount = 1,
            BestWeightKg = 5,
            AverageWeightKg = 5,
            LastUsedWeightKg = 5,
            AverageKcalBurned = 5,
            AverageRateOfPerceivedExertion = 5,
            LastPerformedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        await statsService.RecomputeAllAsync(CancellationToken.None);

        var allStats = await context.UserExerciseStats
            .AsNoTracking()
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.ExerciseId)
            .ToListAsync();

        Assert.Equal(3, allStats.Count);
        Assert.DoesNotContain(allStats, x => x.UserId == 1 && x.ExerciseId == 3);

        var recomputedUser1Exercise1 = allStats.Single(x => x.UserId == 1 && x.ExerciseId == 1);
        Assert.Equal(1, recomputedUser1Exercise1.UseCount);
        Assert.Equal(20d, recomputedUser1Exercise1.BestWeightKg, 6);
        Assert.Equal(20d, recomputedUser1Exercise1.AverageWeightKg, 6);
        Assert.Equal(20d, recomputedUser1Exercise1.LastUsedWeightKg, 6);

        var recomputedUser1Exercise2 = allStats.Single(x => x.UserId == 1 && x.ExerciseId == 2);
        Assert.Equal(1, recomputedUser1Exercise2.UseCount);
        Assert.Equal(70d, recomputedUser1Exercise2.LastUsedWeightKg, 6);
        Assert.NotNull(recomputedUser1Exercise2.AverageTimerInSeconds);
        Assert.Equal(300d, recomputedUser1Exercise2.AverageTimerInSeconds.Value, 6);

        var recomputedUser2Exercise1 = allStats.Single(x => x.UserId == 2 && x.ExerciseId == 1);
        Assert.Equal(1, recomputedUser2Exercise1.UseCount);
        Assert.Equal(50d, recomputedUser2Exercise1.BestWeightKg, 6);
        Assert.Equal(50d, recomputedUser2Exercise1.LastUsedWeightKg, 6);
    }

    private static (WorkoutsService WorkoutsService, UserExerciseStatsService UserExerciseStatsService) CreateServices(
        WorkoutLogDbContext context)
    {
        var statsService = new UserExerciseStatsService(context);
        var workoutsService = new WorkoutsService(context, statsService);
        return (workoutsService, statsService);
    }

    private static WorkoutLogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkoutLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new WorkoutLogDbContext(options);
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

    private static CreateWorkoutRequest BuildWorkoutRequest(
        DateTime performedAtUtc,
        List<WorkoutEntryRequest> entries)
    {
        return new CreateWorkoutRequest
        {
            Feeling = "good",
            DurationInMinutes = 30,
            Mood = 5,
            PerformedAtUtc = performedAtUtc,
            Entries = entries
        };
    }

    private static WorkoutEntryRequest BuildEntry(
        int exerciseId,
        int orderNumber,
        double weightUsedKg,
        int kcalBurned,
        int repetitions = 1,
        double rpe = 5,
        int? timerInSeconds = null,
        int? distanceInMeters = null,
        int? speed = null,
        int? heartRateAvg = null)
    {
        return new WorkoutEntryRequest
        {
            ExerciseId = exerciseId,
            OrderNumber = orderNumber,
            Repetitions = repetitions,
            Mood = 5,
            WeightUsedKg = weightUsedKg,
            KcalBurned = kcalBurned,
            RateOfPerceivedExertion = rpe,
            TimerInSeconds = timerInSeconds,
            DistanceInMeters = distanceInMeters,
            Speed = speed,
            HeartRateAvg = heartRateAvg
        };
    }
}
