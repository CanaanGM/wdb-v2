using Api.Features.Measurements.Contracts;
using Api.Features.Measurements.Services;
using Domain.Measurements;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.Measurements;

public sealed class MeasurementsServiceTests
{
    [Fact]
    public async Task CreateAsync_ComputesBodyWeight_FromBodyCompositionComponents()
    {
        await using var context = CreateContext();
        await SeedUsersAsync(context, [1]);
        var service = new MeasurementsService(context);

        var create = await service.CreateAsync(
            1,
            new MeasurementUpsertRequest
            {
                Minerals = 3,
                Protein = 12,
                TotalBodyWater = 40,
                BodyFatMass = 20,
                BodyWeight = 999
            },
            CancellationToken.None);

        Assert.Equal(MeasurementOperationResultType.Success, create.ResultType);
        Assert.NotNull(create.Value);
        Assert.Equal(75d, create.Value.BodyWeight);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyUserMeasurements_SortedNewestFirst()
    {
        await using var context = CreateContext();
        await SeedUsersAsync(context, [1, 2]);

        context.Measurements.AddRange(
            new Measurement
            {
                UserId = 1,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                BodyWeight = 80
            },
            new Measurement
            {
                UserId = 1,
                CreatedAtUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                BodyWeight = 79
            },
            new Measurement
            {
                UserId = 2,
                CreatedAtUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                BodyWeight = 90
            });
        await context.SaveChangesAsync();

        var service = new MeasurementsService(context);
        var items = await service.GetAllAsync(1, CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal([79d, 80d], items.Select(x => x.BodyWeight).ToList());
    }

    [Fact]
    public async Task UpdateAndDelete_RequireOwnership()
    {
        await using var context = CreateContext();
        await SeedUsersAsync(context, [1, 2]);

        var measurement = new Measurement
        {
            UserId = 2,
            CreatedAtUtc = DateTime.UtcNow,
            BodyWeight = 90
        };
        context.Measurements.Add(measurement);
        await context.SaveChangesAsync();

        var service = new MeasurementsService(context);

        var update = await service.UpdateAsync(
            1,
            measurement.Id,
            new MeasurementUpsertRequest { BodyWeight = 70 },
            CancellationToken.None);
        Assert.Equal(MeasurementOperationResultType.NotFound, update.ResultType);

        var delete = await service.DeleteAsync(1, measurement.Id, CancellationToken.None);
        Assert.Equal(MeasurementOperationResultType.NotFound, delete.ResultType);
    }

    private static WorkoutLogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkoutLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new WorkoutLogDbContext(options);
    }

    private static async Task SeedUsersAsync(WorkoutLogDbContext context, IReadOnlyCollection<int> userIds)
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

        await context.SaveChangesAsync();
    }
}
