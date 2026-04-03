using Api.Features.UserExerciseStats.Services;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;
using Domain.Exercises;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.Workouts;

public sealed class WorkoutsServiceTests
{
    [Fact]
    public async Task CreateAsync_ComputesTotalKgMovedAsWeightTimesRepetitions()
    {
        await using var context = CreateContext();
        await SeedUserAndExercisesAsync(context, userId: 1, exerciseIds: [1, 2]);
        var service = CreateService(context);

        var result = await service.CreateAsync(
            1,
            new CreateWorkoutRequest
            {
                Feeling = "strong",
                DurationInMinutes = 30,
                Mood = 7,
                Entries =
                [
                    BuildEntry(exerciseId: 1, orderNumber: 1, repetitions: 10, weightUsedKg: 100),
                    BuildEntry(exerciseId: 2, orderNumber: 2, repetitions: 5, weightUsedKg: 80)
                ]
            },
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, result.ResultType);
        Assert.NotNull(result.Value);
        Assert.Equal(1400d, result.Value.TotalKgMoved, 6);
        Assert.Equal(15, result.Value.TotalRepetitions);
    }

    [Fact]
    public async Task CreateAsync_PreservesFeelingCasingAndTrimsWhitespace()
    {
        await using var context = CreateContext();
        await SeedUserAndExercisesAsync(context, userId: 1, exerciseIds: [1]);
        var service = CreateService(context);

        var result = await service.CreateAsync(
            1,
            new CreateWorkoutRequest
            {
                Feeling = "  Felt amazing after PR  ",
                DurationInMinutes = 30,
                Mood = 7,
                Entries = [BuildEntry(exerciseId: 1, orderNumber: 1, repetitions: 8, weightUsedKg: 100)]
            },
            CancellationToken.None);

        Assert.Equal(WorkoutOperationResultType.Success, result.ResultType);
        Assert.NotNull(result.Value);
        Assert.Equal("Felt amazing after PR", result.Value.Feeling);

        var storedFeeling = await context.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == 1)
            .Select(x => x.Feeling)
            .SingleAsync();

        Assert.Equal("Felt amazing after PR", storedFeeling);
    }

    private static WorkoutsService CreateService(WorkoutLogDbContext context)
    {
        var statsService = new UserExerciseStatsService(context);
        return new WorkoutsService(context, statsService);
    }

    private static WorkoutLogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkoutLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new WorkoutLogDbContext(options);
    }

    private static async Task SeedUserAndExercisesAsync(
        WorkoutLogDbContext context,
        int userId,
        IReadOnlyCollection<int> exerciseIds)
    {
        context.Users.Add(new AuthUser
        {
            Id = userId,
            UserName = $"user{userId}",
            NormalizedUserName = $"USER{userId}",
            Email = $"user{userId}@example.com",
            NormalizedEmail = $"USER{userId}@EXAMPLE.COM"
        });

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
        double weightUsedKg)
    {
        return new WorkoutEntryRequest
        {
            ExerciseId = exerciseId,
            OrderNumber = orderNumber,
            Repetitions = repetitions,
            Mood = 5,
            WeightUsedKg = weightUsedKg,
            KcalBurned = 10,
            RateOfPerceivedExertion = 6
        };
    }
}
