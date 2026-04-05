using Api.Features.TrainingTypes.Contracts;
using Api.Features.TrainingTypes.Services;
using Domain.TrainingTypes;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.TrainingTypes;

public sealed class TrainingTypesServiceTests
{
    [Fact]
    public async Task CreateBulkAsync_CreatesNormalizedTrainingTypes()
    {
        await using var context = CreateContext();
        var service = new TrainingTypesService(context);

        var result = await service.CreateBulkAsync(
            [
                new CreateTrainingTypeRequest { Name = " Strength " },
                new CreateTrainingTypeRequest { Name = "Hypertrophy" }
            ],
            CancellationToken.None);

        Assert.Equal(TrainingTypeOperationResultType.Success, result.ResultType);
        Assert.Equal(2, result.Value);

        var names = await context.TrainingTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Equal(["hypertrophy", "strength"], names);
    }

    [Fact]
    public async Task CreateBulkAsync_DuplicateNamesInRequest_ReturnsConflict()
    {
        await using var context = CreateContext();
        var service = new TrainingTypesService(context);

        var result = await service.CreateBulkAsync(
            [
                new CreateTrainingTypeRequest { Name = "Strength" },
                new CreateTrainingTypeRequest { Name = " strength " }
            ],
            CancellationToken.None);

        Assert.Equal(TrainingTypeOperationResultType.Conflict, result.ResultType);
        Assert.Equal("Duplicate training types in request: strength.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_ExistingNameConflict_ReturnsConflict()
    {
        await using var context = CreateContext();
        context.TrainingTypes.AddRange(
            new TrainingType { Id = 1, Name = "strength" },
            new TrainingType { Id = 2, Name = "hypertrophy" });
        await context.SaveChangesAsync();

        var service = new TrainingTypesService(context);
        var result = await service.UpdateAsync(
            2,
            new UpdateTrainingTypeRequest { Name = "Strength" },
            CancellationToken.None);

        Assert.Equal(TrainingTypeOperationResultType.Conflict, result.ResultType);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTrainingType()
    {
        await using var context = CreateContext();
        context.TrainingTypes.Add(new TrainingType { Id = 1, Name = "strength" });
        await context.SaveChangesAsync();

        var service = new TrainingTypesService(context);
        var delete = await service.DeleteAsync(1, CancellationToken.None);

        Assert.Equal(TrainingTypeOperationResultType.Success, delete.ResultType);
        Assert.False(await context.TrainingTypes.AnyAsync());
    }

    private static WorkoutLogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkoutLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new WorkoutLogDbContext(options);
    }
}
