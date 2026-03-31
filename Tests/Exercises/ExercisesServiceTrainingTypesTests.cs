using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Services;
using Domain.Muscles;
using Domain.TrainingTypes;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.Exercises;

public sealed class ExercisesServiceTrainingTypesTests
{
    [Fact]
    public async Task CreateAsync_AssignsTrainingTypes_AndSearchFiltersByTrainingType()
    {
        await using var context = CreateContext();
        context.Muscles.Add(new Muscle
        {
            Id = 1,
            Name = "chest",
            MuscleGroup = "upper-body"
        });
        context.TrainingTypes.AddRange(
            new TrainingType { Id = 1, Name = "strength" },
            new TrainingType { Id = 2, Name = "hypertrophy" });
        await context.SaveChangesAsync();

        var service = new ExercisesService(context);
        var create = await service.CreateAsync(
            new CreateExerciseRequest
            {
                Name = "Bench Press",
                Difficulty = 2,
                TrainingTypes = [" Strength ", "hypertrophy"],
                HowTos = [],
                ExerciseMuscles =
                [
                    new CreateExerciseMuscleRequest
                    {
                        MuscleName = "Chest",
                        IsPrimary = true
                    }
                ]
            },
            CancellationToken.None);

        Assert.Equal(CreateExerciseResultType.Success, create.ResultType);
        Assert.NotNull(create.Exercise);
        Assert.Equal(["hypertrophy", "strength"], create.Exercise.TrainingTypes.OrderBy(x => x).ToList());

        var strengthSearch = await service.GetAllAsync(
            new GetExercisesRequest
            {
                TrainingTypeName = "strength",
                PageNumber = 1,
                PageSize = 20
            },
            CancellationToken.None);

        Assert.Single(strengthSearch.Items);
        Assert.Equal("bench press", strengthSearch.Items[0].Name);
    }

    [Fact]
    public async Task CreateAsync_MissingTrainingType_ReturnsValidationError()
    {
        await using var context = CreateContext();
        context.Muscles.Add(new Muscle
        {
            Id = 1,
            Name = "chest",
            MuscleGroup = "upper-body"
        });
        await context.SaveChangesAsync();

        var service = new ExercisesService(context);
        var create = await service.CreateAsync(
            new CreateExerciseRequest
            {
                Name = "Bench Press",
                Difficulty = 2,
                TrainingTypes = ["strength"],
                HowTos = [],
                ExerciseMuscles =
                [
                    new CreateExerciseMuscleRequest
                    {
                        MuscleName = "chest",
                        IsPrimary = true
                    }
                ]
            },
            CancellationToken.None);

        Assert.Equal(CreateExerciseResultType.ValidationError, create.ResultType);
        Assert.Equal("Training types not found: strength.", create.Error);
    }

    private static WorkoutLogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkoutLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new WorkoutLogDbContext(options);
    }
}
