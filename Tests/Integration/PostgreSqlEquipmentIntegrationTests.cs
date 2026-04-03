using Api.Features.Equipments.Services;
using Domain.Equipments;
using Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.Integration;

[Collection(IntegrationTestCollections.PostgreSql)]
public sealed class PostgreSqlEquipmentIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task DeleteByNameAsync_WhenLinked_DeletesJoinRowsAndEquipment_OnPostgres()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        await SeedExerciseAndEquipmentAsync(context);
        var service = new EquipmentsService(context);

        var result = await service.DeleteByNameAsync("  BARBELL  ", CancellationToken.None);

        Assert.Equal(DeleteEquipmentResultType.Success, result.ResultType);
        Assert.False(await context.Equipments.AsNoTracking().AnyAsync(x => x.Name == "barbell"));
        Assert.False(await context.ExerciseEquipments.AsNoTracking().AnyAsync());
        Assert.True(await context.Exercises.AsNoTracking().AnyAsync(x => x.Id == 1));
    }

    [Fact]
    public async Task DeleteByNameAsync_WhenMissing_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        var service = new EquipmentsService(context);

        var result = await service.DeleteByNameAsync("missing", CancellationToken.None);

        Assert.Equal(DeleteEquipmentResultType.NotFound, result.ResultType);
        Assert.Equal("Equipment with name 'missing' was not found.", result.Error);
    }

    private static async Task SeedExerciseAndEquipmentAsync(Infrastructure.Persistence.WorkoutLogDbContext context)
    {
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
    }
}
