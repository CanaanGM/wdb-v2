using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;
using Domain.Equipments;
using Domain.Exercises;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.Equipments;

public sealed class EquipmentsServiceTests
{
    [Fact]
    public async Task DeleteByNameAsync_ExistingEquipment_ReturnsSuccess_AndRemovesRow()
    {
        await using var context = CreateContext();
        context.Equipments.Add(new Equipment { Id = 1, Name = "barbell", WeightKg = 20 });
        await context.SaveChangesAsync();

        var service = new EquipmentsService(context);
        var result = await service.DeleteByNameAsync("barbell", CancellationToken.None);

        Assert.Equal(DeleteEquipmentResultType.Success, result.ResultType);
        Assert.False(await context.Equipments.AnyAsync());
    }

    [Fact]
    public async Task DeleteByNameAsync_NormalizesInputName_DeletesNormalizedMatch()
    {
        await using var context = CreateContext();
        context.Equipments.Add(new Equipment { Id = 1, Name = "barbell", WeightKg = 20 });
        await context.SaveChangesAsync();

        var service = new EquipmentsService(context);
        var result = await service.DeleteByNameAsync("  BARBELL  ", CancellationToken.None);

        Assert.Equal(DeleteEquipmentResultType.Success, result.ResultType);
        Assert.False(await context.Equipments.AnyAsync());
    }

    [Fact]
    public async Task DeleteByNameAsync_MissingEquipment_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var service = new EquipmentsService(context);

        var result = await service.DeleteByNameAsync("missing", CancellationToken.None);

        Assert.Equal(DeleteEquipmentResultType.NotFound, result.ResultType);
        Assert.Equal("Equipment with name 'missing' was not found.", result.Error);
    }

    [Fact]
    public async Task DeleteByNameAsync_LinkedEquipment_RemovesJoinRows_ThenDeletesEquipment()
    {
        await using var context = CreateContext();
        context.Exercises.Add(new Exercise
        {
            Id = 1,
            Name = "squat",
            Difficulty = 1
        });
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            Name = "barbell",
            WeightKg = 20
        });
        context.ExerciseEquipments.Add(new ExerciseEquipment
        {
            ExerciseId = 1,
            EquipmentId = 1
        });
        await context.SaveChangesAsync();

        var service = new EquipmentsService(context);
        var result = await service.DeleteByNameAsync("barbell", CancellationToken.None);

        Assert.Equal(DeleteEquipmentResultType.Success, result.ResultType);
        Assert.False(await context.Equipments.AnyAsync());
        Assert.False(await context.ExerciseEquipments.AnyAsync());
        Assert.True(await context.Exercises.AnyAsync(x => x.Id == 1));
    }

    [Fact]
    public async Task DeleteByNameAsync_BlankName_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var service = new EquipmentsService(context);

        var result = await service.DeleteByNameAsync("   ", CancellationToken.None);

        Assert.Equal(DeleteEquipmentResultType.ValidationError, result.ResultType);
        Assert.Equal("Equipment name is required.", result.Error);
    }

    [Fact]
    public async Task ExistingCreatePaths_RemainUnchanged_AfterDeleteAddition()
    {
        await using var context = CreateContext();
        var service = new EquipmentsService(context);

        var create = await service.CreateAsync(
            new CreateEquipmentRequest
            {
                Name = "barbell",
                Description = "desc",
                WeightKg = 20
            },
            CancellationToken.None);

        Assert.Equal(CreateEquipmentResultType.Success, create.ResultType);

        var bulk = await service.CreateBulkAsync(
            [
                new CreateEquipmentRequest { Name = "dumbbell", WeightKg = 10 },
                new CreateEquipmentRequest { Name = "kettlebell", WeightKg = 16 }
            ],
            CancellationToken.None);

        Assert.Equal(CreateEquipmentsBulkResultType.Success, bulk.ResultType);
    }

    private static WorkoutLogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkoutLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new WorkoutLogDbContext(options);
    }
}
