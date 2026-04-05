using Api.Features.Sync.Contracts;
using Api.Features.Sync.Services;
using Domain.Exercises;
using Domain.Plans;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.Integration;

[Collection(IntegrationTestCollections.PostgreSql)]
public sealed class PostgreSqlSyncIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task PushCreateMeasurement_ThenPull_ReturnsCreateChange()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        await SeedUserAsync(context, 1);
        var syncService = new SyncService(context);

        var push = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "measurement",
                        Action = "create",
                        Payload = new Dictionary<string, object?>
                        {
                            ["hip"] = 92.4,
                            ["body_weight"] = 79.7
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var pushResult = Assert.Single(push.Results);
        Assert.Equal("applied", pushResult.Status);
        Assert.NotNull(pushResult.EntityPublicId);
        Assert.NotNull(pushResult.ServerVersion);

        var pull = await syncService.PullAsync(1, 0, 100, CancellationToken.None);
        Assert.NotEmpty(pull.Changes);
        Assert.Contains(
            pull.Changes,
            x => x.EntityType == "measurement"
                 && x.Action == "create"
                 && x.EntityPublicId == pushResult.EntityPublicId);
    }

    [Fact]
    public async Task PushUpdateWithStaleVersion_ReturnsConflictWithCanonicalPayload()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        await SeedUserAsync(context, 1);
        var syncService = new SyncService(context);

        var create = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "measurement",
                        Action = "create",
                        Payload = new Dictionary<string, object?>
                        {
                            ["body_weight"] = 80.0
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var createResult = Assert.Single(create.Results);
        var publicId = Assert.IsType<Guid>(createResult.EntityPublicId);
        var baseVersion = Assert.IsType<long>(createResult.ServerVersion);

        var firstUpdate = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "measurement",
                        Action = "update",
                        EntityPublicId = publicId,
                        BaseVersion = baseVersion,
                        Payload = new Dictionary<string, object?>
                        {
                            ["body_weight"] = 81.2
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var firstUpdateResult = Assert.Single(firstUpdate.Results);
        Assert.Equal("applied", firstUpdateResult.Status);
        Assert.True(firstUpdateResult.ServerVersion > baseVersion);

        var staleUpdate = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "measurement",
                        Action = "update",
                        EntityPublicId = publicId,
                        BaseVersion = baseVersion,
                        Payload = new Dictionary<string, object?>
                        {
                            ["body_weight"] = 82.9
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var staleResult = Assert.Single(staleUpdate.Results);
        Assert.Equal("conflict", staleResult.Status);
        Assert.Equal("version_conflict", staleResult.ErrorCode);
        Assert.NotNull(staleResult.CanonicalPayload);
    }

    [Fact]
    public async Task PushWorkoutBlockAndExercise_ThenPull_ReturnsBothEntityChanges()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        await SeedUserAsync(context, 1);
        await SeedExerciseAsync(context, 100);
        var syncService = new SyncService(context);

        var createBlock = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "workout_block",
                        Action = "create",
                        Payload = new Dictionary<string, object?>
                        {
                            ["name"] = "strength block",
                            ["sets"] = 3,
                            ["rest_in_seconds"] = 90,
                            ["order_number"] = 0
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var blockResult = Assert.Single(createBlock.Results);
        Assert.Equal("applied", blockResult.Status);
        Assert.NotNull(blockResult.EntityPublicId);
        Assert.NotNull(blockResult.CanonicalPayload);
        var blockId = ReadInt(blockResult.CanonicalPayload!, "id");
        Assert.True(blockId > 0);

        var createBlockExercise = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "workout_block_exercise",
                        Action = "create",
                        Payload = new Dictionary<string, object?>
                        {
                            ["workout_block_id"] = blockId,
                            ["exercise_id"] = 100,
                            ["order_number"] = 0,
                            ["repetitions"] = 10
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var blockExerciseResult = Assert.Single(createBlockExercise.Results);
        Assert.Equal("applied", blockExerciseResult.Status);

        var pull = await syncService.PullAsync(1, 0, 200, CancellationToken.None);
        Assert.Contains(pull.Changes, change => change.EntityType == "workout_block");
        Assert.Contains(pull.Changes, change => change.EntityType == "workout_block_exercise");
    }

    [Fact]
    public async Task PushPlanEnrollmentDayAndExerciseExecution_ThenPull_ReturnsPlanEntityChanges()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        await SeedUserAsync(context, 1);
        await SeedExerciseAsync(context, 200);
        var (planTemplateId, planDayExerciseId) = await SeedPlanTemplateGraphAsync(context, 200);
        var syncService = new SyncService(context);

        var createEnrollment = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "user_plan_enrollment",
                        Action = "create",
                        Payload = new Dictionary<string, object?>
                        {
                            ["plan_template_id"] = planTemplateId,
                            ["started_at_utc"] = DateTime.UtcNow,
                            ["time_zone_id"] = "UTC",
                            ["start_local_date"] = "2026-04-01",
                            ["end_local_date_inclusive"] = "2026-04-30",
                            ["status"] = "active",
                            ["display_order"] = 0
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var enrollmentResult = Assert.Single(createEnrollment.Results);
        Assert.Equal("applied", enrollmentResult.Status);
        Assert.NotNull(enrollmentResult.CanonicalPayload);
        var enrollmentId = ReadInt(enrollmentResult.CanonicalPayload!, "id");
        Assert.True(enrollmentId > 0);

        var createDayExecution = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "user_plan_day_execution",
                        Action = "create",
                        Payload = new Dictionary<string, object?>
                        {
                            ["enrollment_id"] = enrollmentId,
                            ["local_date"] = "2026-04-02",
                            ["status"] = "scheduled"
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var dayExecutionResult = Assert.Single(createDayExecution.Results);
        Assert.Equal("applied", dayExecutionResult.Status);
        Assert.NotNull(dayExecutionResult.CanonicalPayload);
        var dayExecutionId = ReadInt(dayExecutionResult.CanonicalPayload!, "id");
        Assert.True(dayExecutionId > 0);

        var createExerciseExecution = await syncService.PushAsync(
            1,
            new SyncPushRequest
            {
                DeviceId = "device-1",
                Operations =
                [
                    new SyncOperationRequest
                    {
                        OpId = Guid.NewGuid(),
                        EntityType = "user_plan_exercise_execution",
                        Action = "create",
                        Payload = new Dictionary<string, object?>
                        {
                            ["day_execution_id"] = dayExecutionId,
                            ["plan_day_exercise_id"] = planDayExerciseId,
                            ["status"] = "pending"
                        },
                        OccurredAtUtc = DateTime.UtcNow
                    }
                ]
            },
            CancellationToken.None);

        var exerciseExecutionResult = Assert.Single(createExerciseExecution.Results);
        Assert.Equal("applied", exerciseExecutionResult.Status);

        var pull = await syncService.PullAsync(1, 0, 300, CancellationToken.None);
        Assert.Contains(pull.Changes, change => change.EntityType == "user_plan_enrollment");
        Assert.Contains(pull.Changes, change => change.EntityType == "user_plan_day_execution");
        Assert.Contains(pull.Changes, change => change.EntityType == "user_plan_exercise_execution");
    }

    private static async Task SeedUserAsync(Infrastructure.Persistence.WorkoutLogDbContext context, int userId)
    {
        context.Users.Add(new AuthUser
        {
            Id = userId,
            UserName = $"user{userId}",
            NormalizedUserName = $"USER{userId}",
            Email = $"user{userId}@example.com",
            NormalizedEmail = $"USER{userId}@EXAMPLE.COM"
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedExerciseAsync(Infrastructure.Persistence.WorkoutLogDbContext context, int exerciseId)
    {
        context.Exercises.Add(new Exercise
        {
            Id = exerciseId,
            Name = $"exercise-{exerciseId}",
            Difficulty = 1
        });

        await context.SaveChangesAsync();
    }

    private static async Task<(int PlanTemplateId, int PlanDayExerciseId)> SeedPlanTemplateGraphAsync(
        Infrastructure.Persistence.WorkoutLogDbContext context,
        int exerciseId)
    {
        var template = new PlanTemplate
        {
            Slug = "starter-template",
            Name = "Starter Template",
            Description = "template for sync tests",
            DurationWeeks = 4,
            Status = "published",
            Version = 1
        };
        context.PlanTemplates.Add(template);
        await context.SaveChangesAsync();

        var day = new PlanDay
        {
            PlanTemplateId = template.Id,
            WeekNumber = 1,
            DayNumber = 1,
            Title = "Day 1",
            Notes = "sync test day"
        };
        context.PlanDays.Add(day);
        await context.SaveChangesAsync();

        var planExercise = new PlanDayExercise
        {
            PlanDayId = day.Id,
            ExerciseId = exerciseId,
            OrderNumber = 0,
            Sets = 3,
            Repetitions = 10,
            TargetRateOfPerceivedExertion = 7
        };
        context.PlanDayExercises.Add(planExercise);
        await context.SaveChangesAsync();

        return (template.Id, planExercise.Id);
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> payload, string key)
    {
        if (!payload.TryGetValue(key, out var raw) || raw is null)
        {
            return 0;
        }

        if (raw is int valueInt)
        {
            return valueInt;
        }

        if (raw is long valueLong)
        {
            return (int)valueLong;
        }

        if (raw is decimal valueDecimal)
        {
            return (int)valueDecimal;
        }

        if (raw is double valueDouble)
        {
            return (int)valueDouble;
        }

        return int.TryParse(raw.ToString(), out var parsed) ? parsed : 0;
    }
}
