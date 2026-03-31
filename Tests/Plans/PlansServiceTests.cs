using Api.Features.Plans.Contracts;
using Api.Features.Plans.Services;
using Domain.Equipments;
using Domain.Exercises;
using Domain.Plans;
using Domain.TrainingTypes;
using Domain.Workouts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutLog.Tests.Plans;

public sealed class PlansServiceTests
{
    [Fact]
    public async Task CreatePlanAsync_CreatesDraftPlan_WithNormalizedAndOrderedSchedule()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2, 3]);
        var service = CreateService(context);

        var result = await service.CreatePlanAsync(
            new CreatePlanTemplateRequest
            {
                Slug = "  SaVaGe  ",
                Name = "  Savage Plan  ",
                Description = "  Build strength  ",
                DurationWeeks = 2,
                Days =
                [
                    BuildPlanDay(2, 1, [BuildPlanExercise(3, 2), BuildPlanExercise(2, 1)]),
                    BuildPlanDay(1, 2, [BuildPlanExercise(1, 1)])
                ]
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, result.ResultType);
        Assert.NotNull(result.Value);

        var created = result.Value;
        Assert.Equal("savage", created.Slug);
        Assert.Equal("Savage Plan", created.Name);
        Assert.Equal("Build strength", created.Description);
        Assert.Equal("draft", created.Status);
        Assert.Equal(1, created.Version);
        Assert.Equal(2, created.Days.Count);
        Assert.Equal((1, 2), (created.Days[0].WeekNumber, created.Days[0].DayNumber));
        Assert.Equal((2, 1), (created.Days[1].WeekNumber, created.Days[1].DayNumber));
        Assert.Equal([1], created.Days[0].Exercises.Select(x => x.ExerciseId).ToList());
        Assert.Equal([2, 3], created.Days[1].Exercises.Select(x => x.ExerciseId).ToList());
    }

    [Fact]
    public async Task CreatePlanAsync_WeekBeyondDuration_ReturnsValidationError()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        var result = await service.CreatePlanAsync(
            new CreatePlanTemplateRequest
            {
                Slug = "starter",
                Name = "Starter",
                DurationWeeks = 1,
                Days = [BuildPlanDay(2, 1, [BuildPlanExercise(1, 1)])]
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, result.ResultType);
        Assert.Equal("Plan day week number cannot exceed durationWeeks.", result.Error);
    }

    [Fact]
    public async Task CreatePlanAsync_MissingExercise_ReturnsValidationError()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        var result = await service.CreatePlanAsync(
            new CreatePlanTemplateRequest
            {
                Slug = "starter",
                Name = "Starter",
                DurationWeeks = 1,
                Days = [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1), BuildPlanExercise(99, 2)])]
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, result.ResultType);
        Assert.Equal("Exercises not found: 99.", result.Error);
    }

    [Fact]
    public async Task CreatePlansBulkAsync_AssignsSequentialVersionsPerSlug()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        context.PlanTemplates.Add(
            new PlanTemplate
            {
                Slug = "alpha",
                Name = "Existing Alpha",
                DurationWeeks = 1,
                Status = "published",
                Version = 2,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var result = await service.CreatePlansBulkAsync(
            [
                new CreatePlanTemplateRequest
                {
                    Slug = " Alpha ",
                    Name = "Alpha 3",
                    DurationWeeks = 1,
                    Days = [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]
                },
                new CreatePlanTemplateRequest
                {
                    Slug = "beta",
                    Name = "Beta 1",
                    DurationWeeks = 1,
                    Days = [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]
                },
                new CreatePlanTemplateRequest
                {
                    Slug = "alpha",
                    Name = "Alpha 4",
                    DurationWeeks = 1,
                    Days = [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]
                }
            ],
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, result.ResultType);
        Assert.Equal(3, result.Value);

        var alphaVersions = await context.PlanTemplates
            .AsNoTracking()
            .Where(x => x.Slug == "alpha")
            .OrderBy(x => x.Version)
            .Select(x => x.Version)
            .ToListAsync();
        var betaVersions = await context.PlanTemplates
            .AsNoTracking()
            .Where(x => x.Slug == "beta")
            .OrderBy(x => x.Version)
            .Select(x => x.Version)
            .ToListAsync();

        Assert.Equal([2, 3, 4], alphaVersions);
        Assert.Equal([1], betaVersions);
    }

    [Fact]
    public async Task UpdatePlanAsync_ReplacesScheduleAndPublishesPlan()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2, 3]);
        var service = CreateService(context);

        var created = await service.CreatePlanAsync(
            new CreatePlanTemplateRequest
            {
                Slug = "power",
                Name = "Power",
                DurationWeeks = 1,
                Days = [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, created.ResultType);
        Assert.NotNull(created.Value);

        var updated = await service.UpdatePlanAsync(
            created.Value.Id,
            new UpdatePlanTemplateRequest
            {
                Name = "Power Updated",
                Description = "  upgraded  ",
                DurationWeeks = 2,
                Status = "published",
                Days =
                [
                    BuildPlanDay(2, 3, [BuildPlanExercise(2, 2), BuildPlanExercise(3, 1)])
                ]
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, updated.ResultType);
        Assert.NotNull(updated.Value);

        var plan = updated.Value;
        Assert.Equal("published", plan.Status);
        Assert.Equal(2, plan.DurationWeeks);
        Assert.Equal("upgraded", plan.Description);
        Assert.Single(plan.Days);
        Assert.Equal((2, 3), (plan.Days[0].WeekNumber, plan.Days[0].DayNumber));
        Assert.Equal([3, 2], plan.Days[0].Exercises.Select(x => x.ExerciseId).ToList());

        var dayCount = await context.PlanDays.CountAsync(x => x.PlanTemplateId == plan.Id);
        var exerciseCount = await context.PlanDayExercises.CountAsync(x => x.PlanDay.PlanTemplateId == plan.Id);
        Assert.Equal(1, dayCount);
        Assert.Equal(2, exerciseCount);
    }

    [Fact]
    public async Task CreatePlanDayExercisesBulkAsync_DuplicateOrderNumbers_ReturnsValidationError()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "bulk-a",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);

        var result = await service.CreatePlanDayExercisesBulkAsync(
            planId,
            weekNumber: 1,
            dayNumber: 1,
            [BuildPlanExercise(1, 2), BuildPlanExercise(2, 2)],
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, result.ResultType);
        Assert.Equal("Duplicate exercise order numbers: 2.", result.Error);
    }

    [Fact]
    public async Task CreatePlanDayExercisesBulkAsync_OrderConflictWithExisting_ReturnsValidationError()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "bulk-b",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);

        var result = await service.CreatePlanDayExercisesBulkAsync(
            planId,
            weekNumber: 1,
            dayNumber: 1,
            [BuildPlanExercise(2, 1)],
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, result.ResultType);
        Assert.Equal("Order numbers already exist in plan day: 1.", result.Error);
    }

    [Fact]
    public async Task CreatePlanDayExercisesBulkAsync_AppendsExercises_WhenValid()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2, 3]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "bulk-c",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);

        var result = await service.CreatePlanDayExercisesBulkAsync(
            planId,
            weekNumber: 1,
            dayNumber: 1,
            [BuildPlanExercise(2, 2), BuildPlanExercise(3, 3)],
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, result.ResultType);
        Assert.Equal(2, result.Value);

        var storedOrders = await context.PlanDayExercises
            .AsNoTracking()
            .Where(x => x.PlanDay.PlanTemplateId == planId && x.PlanDay.WeekNumber == 1 && x.PlanDay.DayNumber == 1)
            .OrderBy(x => x.OrderNumber)
            .Select(x => x.OrderNumber)
            .ToListAsync();

        Assert.Equal([1, 2, 3], storedOrders);
    }

    [Fact]
    public async Task SearchPlansAsync_StatusNull_ReturnsPagedResultsSortedByNewest()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        context.PlanTemplates.AddRange(
            new PlanTemplate
            {
                Slug = "a",
                Name = "A",
                DurationWeeks = 1,
                Status = "published",
                Version = 1,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PlanTemplate
            {
                Slug = "b",
                Name = "B",
                DurationWeeks = 1,
                Status = "draft",
                Version = 1,
                CreatedAtUtc = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            },
            new PlanTemplate
            {
                Slug = "c",
                Name = "C",
                DurationWeeks = 1,
                Status = "published",
                Version = 1,
                CreatedAtUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var page = await service.SearchPlansAsync(
            new SearchPlansRequest
            {
                Status = null,
                PageNumber = 1,
                PageSize = 2
            },
            CancellationToken.None);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(["b", "c"], page.Items.Select(x => x.Slug).ToList());
    }

    [Fact]
    public async Task GetPlanByIdAsync_ReturnsDerivedTrainingTypesAndRequiredEquipment()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2]);

        var strength = new TrainingType { Id = 1, Name = "strength" };
        var conditioning = new TrainingType { Id = 2, Name = "conditioning" };
        var barbell = new Equipment { Id = 1, Name = "barbell", WeightKg = 20 };
        var dumbbell = new Equipment { Id = 2, Name = "dumbbell", WeightKg = 10 };

        context.TrainingTypes.AddRange(strength, conditioning);
        context.Equipments.AddRange(barbell, dumbbell);

        context.ExerciseTrainingTypes.AddRange(
            new ExerciseTrainingType { ExerciseId = 1, TrainingTypeId = 1 },
            new ExerciseTrainingType { ExerciseId = 2, TrainingTypeId = 2 });

        context.ExerciseEquipments.AddRange(
            new ExerciseEquipment { ExerciseId = 1, EquipmentId = 1 },
            new ExerciseEquipment { ExerciseId = 1, EquipmentId = 2 },
            new ExerciseEquipment { ExerciseId = 2, EquipmentId = 1 });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var create = await service.CreatePlanAsync(
            new CreatePlanTemplateRequest
            {
                Slug = "derived-fields",
                Name = "Derived Fields",
                DurationWeeks = 1,
                Days =
                [
                    BuildPlanDay(1, 1, [BuildPlanExercise(1, 1), BuildPlanExercise(2, 2)])
                ]
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, create.ResultType);
        Assert.NotNull(create.Value);

        var details = await service.GetPlanByIdAsync(create.Value.Id, CancellationToken.None);
        Assert.NotNull(details);
        Assert.Equal(["conditioning", "strength"], details.TrainingTypes);
        Assert.Equal(["barbell", "dumbbell"], details.RequiredEquipment);
    }

    [Fact]
    public async Task EnrollAsync_RequiresPublishedPlan()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        var created = await service.CreatePlanAsync(
            new CreatePlanTemplateRequest
            {
                Slug = "draft-plan",
                Name = "Draft Plan",
                DurationWeeks = 1,
                Days = [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, created.ResultType);
        Assert.NotNull(created.Value);

        var enroll = await service.EnrollAsync(
            1,
            created.Value.Id,
            new EnrollInPlanRequest { TimeZoneId = "UTC" },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, enroll.ResultType);
        Assert.Equal("Only published plans can be enrolled.", enroll.Error);
    }

    [Fact]
    public async Task EnrollAsync_ComputesLocalDateRange_AndPreventsDuplicateActiveEnrollment()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        var plan1Id = await CreatePublishedPlanAsync(
            service,
            slug: "first-plan",
            durationWeeks: 2,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);
        var plan2Id = await CreatePublishedPlanAsync(
            service,
            slug: "second-plan",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);

        var startedAtUtc = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var firstEnrollment = await service.EnrollAsync(
            1,
            plan1Id,
            new EnrollInPlanRequest
            {
                StartedAtUtc = startedAtUtc,
                TimeZoneId = "UTC"
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, firstEnrollment.ResultType);
        Assert.NotNull(firstEnrollment.Value);
        Assert.Equal(new DateOnly(2026, 4, 1), firstEnrollment.Value.StartLocalDate);
        Assert.Equal(new DateOnly(2026, 4, 14), firstEnrollment.Value.EndLocalDateInclusive);
        Assert.Equal(0, firstEnrollment.Value.DisplayOrder);

        var secondEnrollment = await service.EnrollAsync(
            1,
            plan2Id,
            new EnrollInPlanRequest { TimeZoneId = "UTC" },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, secondEnrollment.ResultType);
        Assert.NotNull(secondEnrollment.Value);
        Assert.Equal(1, secondEnrollment.Value.DisplayOrder);

        var duplicate = await service.EnrollAsync(
            1,
            plan1Id,
            new EnrollInPlanRequest { TimeZoneId = "UTC" },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, duplicate.ResultType);
        Assert.Equal("You already have an active enrollment for this plan.", duplicate.Error);
    }

    [Fact]
    public async Task SearchEnrollmentsAsync_FiltersByStatusPerUser()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1, 2], [1]);
        var service = CreateService(context);

        var planAId = await CreatePublishedPlanAsync(
            service,
            slug: "plan-a",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);
        var planBId = await CreatePublishedPlanAsync(
            service,
            slug: "plan-b",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);

        context.UserPlanEnrollments.AddRange(
            new UserPlanEnrollment
            {
                UserId = 1,
                PlanTemplateId = planAId,
                StartedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                TimeZoneId = "UTC",
                StartLocalDate = new DateOnly(2026, 5, 1),
                EndLocalDateInclusive = new DateOnly(2026, 5, 7),
                Status = "active",
                DisplayOrder = 0,
                CreatedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new UserPlanEnrollment
            {
                UserId = 1,
                PlanTemplateId = planBId,
                StartedAtUtc = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                TimeZoneId = "UTC",
                StartLocalDate = new DateOnly(2026, 5, 2),
                EndLocalDateInclusive = new DateOnly(2026, 5, 8),
                Status = "cancelled",
                DisplayOrder = 1,
                CreatedAtUtc = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new UserPlanEnrollment
            {
                UserId = 2,
                PlanTemplateId = planAId,
                StartedAtUtc = new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc),
                TimeZoneId = "UTC",
                StartLocalDate = new DateOnly(2026, 5, 3),
                EndLocalDateInclusive = new DateOnly(2026, 5, 9),
                Status = "active",
                DisplayOrder = 0,
                CreatedAtUtc = new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var response = await service.SearchEnrollmentsAsync(
            1,
            new SearchUserPlanEnrollmentsRequest
            {
                Status = "active",
                PageNumber = 1,
                PageSize = 20
            },
            CancellationToken.None);

        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
        Assert.Equal(planAId, response.Items[0].PlanTemplateId);
        Assert.Equal("active", response.Items[0].Status);
    }

    [Fact]
    public async Task GetAgendaAsync_ReturnsScheduledAndRestDaysWithExecutionStatuses()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "agenda-plan",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 2), BuildPlanExercise(2, 1)])]);

        var enrollmentResult = await service.EnrollAsync(
            1,
            planId,
            new EnrollInPlanRequest
            {
                StartedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TimeZoneId = "UTC"
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, enrollmentResult.ResultType);
        Assert.NotNull(enrollmentResult.Value);
        var enrollmentId = enrollmentResult.Value.Id;

        var planDay = await context.PlanDays
            .AsNoTracking()
            .Include(x => x.Exercises)
            .SingleAsync(x => x.PlanTemplateId == planId && x.WeekNumber == 1 && x.DayNumber == 1);
        var completedExerciseId = planDay.Exercises.Single(x => x.OrderNumber == 1).Id;

        var firstDate = new DateOnly(2026, 6, 1);
        var restDate = new DateOnly(2026, 6, 2);
        context.UserPlanDayExecutions.AddRange(
            new UserPlanDayExecution
            {
                EnrollmentId = enrollmentId,
                LocalDate = firstDate,
                Status = "partial",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ExerciseExecutions =
                [
                    new UserPlanExerciseExecution
                    {
                        PlanDayExerciseId = completedExerciseId,
                        Status = "completed",
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    }
                ]
            },
            new UserPlanDayExecution
            {
                EnrollmentId = enrollmentId,
                LocalDate = restDate,
                Status = "skipped",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var agenda = await service.GetAgendaAsync(
            1,
            new GetUserPlanAgendaRequest
            {
                FromLocalDate = firstDate,
                ToLocalDate = restDate
            },
            CancellationToken.None);

        Assert.Equal(2, agenda.Count);

        var scheduledDay = agenda.Single(x => x.LocalDate == firstDate);
        Assert.False(scheduledDay.IsRestDay);
        Assert.Equal("partial", scheduledDay.Status);
        Assert.Equal([1, 2], scheduledDay.Exercises.Select(x => x.OrderNumber).ToList());
        Assert.Equal(["completed", "pending"], scheduledDay.Exercises.Select(x => x.Status).ToList());

        var restDay = agenda.Single(x => x.LocalDate == restDate);
        Assert.True(restDay.IsRestDay);
        Assert.Equal("skipped", restDay.Status);
        Assert.Empty(restDay.Exercises);
    }

    [Fact]
    public async Task CompleteDayAsync_ValidatesWorkoutOwnership_AndUpsertsExecution()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1, 2], [1]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "complete-day",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);
        var enrollmentId = await EnrollUserAsync(service, 1, planId, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        var (ownedSession, _) = await SeedWorkoutAsync(context, 1, 1);
        var (foreignSession, _) = await SeedWorkoutAsync(context, 2, 1);

        var localDate = new DateOnly(2026, 7, 1);
        var invalid = await service.CompleteDayAsync(
            1,
            enrollmentId,
            new CompletePlanDayRequest
            {
                LocalDate = localDate,
                LinkedWorkoutSessionId = foreignSession.Id
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, invalid.ResultType);
        Assert.Equal("linkedWorkoutSessionId must reference one of your workouts.", invalid.Error);

        var completed = await service.CompleteDayAsync(
            1,
            enrollmentId,
            new CompletePlanDayRequest
            {
                LocalDate = localDate,
                LinkedWorkoutSessionId = ownedSession.Id,
                Notes = "  done  "
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, completed.ResultType);
        Assert.NotNull(completed.Value);
        Assert.Equal("completed", completed.Value.Status);
        Assert.Equal(ownedSession.Id, completed.Value.LinkedWorkoutSessionId);
        Assert.Equal("done", completed.Value.Notes);

        var updated = await service.CompleteDayAsync(
            1,
            enrollmentId,
            new CompletePlanDayRequest
            {
                LocalDate = localDate,
                LinkedWorkoutSessionId = null,
                Notes = "  updated  "
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, updated.ResultType);
        Assert.NotNull(updated.Value);
        Assert.Equal("completed", updated.Value.Status);
        Assert.Null(updated.Value.LinkedWorkoutSessionId);
        Assert.Equal("updated", updated.Value.Notes);

        var storedExecutions = await context.UserPlanDayExecutions
            .AsNoTracking()
            .Where(x => x.EnrollmentId == enrollmentId && x.LocalDate == localDate)
            .ToListAsync();

        Assert.Single(storedExecutions);
    }

    [Fact]
    public async Task SkipDayAsync_MarksDaySkipped_AndClearsLinkedWorkout()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "skip-day",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);
        var enrollmentId = await EnrollUserAsync(service, 1, planId, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        var (session, _) = await SeedWorkoutAsync(context, 1, 1);
        var localDate = new DateOnly(2026, 8, 1);
        context.UserPlanDayExecutions.Add(
            new UserPlanDayExecution
            {
                EnrollmentId = enrollmentId,
                LocalDate = localDate,
                Status = "completed",
                LinkedWorkoutSessionId = session.Id,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var result = await service.SkipDayAsync(
            1,
            enrollmentId,
            new SkipPlanDayRequest
            {
                LocalDate = localDate,
                Notes = "  rest  "
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, result.ResultType);
        Assert.NotNull(result.Value);
        Assert.Equal("skipped", result.Value.Status);
        Assert.Null(result.Value.LinkedWorkoutSessionId);
        Assert.Equal("rest", result.Value.Notes);
    }

    [Fact]
    public async Task CompleteExerciseAsync_TransitionsDayFromPartialToCompleted()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "complete-exercise",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1), BuildPlanExercise(2, 2)])]);
        var enrollmentId = await EnrollUserAsync(service, 1, planId, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        var planDayExercises = await context.PlanDayExercises
            .AsNoTracking()
            .Where(x => x.PlanDay.PlanTemplateId == planId && x.PlanDay.WeekNumber == 1 && x.PlanDay.DayNumber == 1)
            .OrderBy(x => x.OrderNumber)
            .ToListAsync();
        var localDate = new DateOnly(2026, 9, 1);

        var first = await service.CompleteExerciseAsync(
            1,
            enrollmentId,
            new CompletePlanExerciseRequest
            {
                LocalDate = localDate,
                PlanDayExerciseId = planDayExercises[0].Id
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, first.ResultType);
        Assert.NotNull(first.Value);
        Assert.Equal("completed", first.Value.Status);

        var partialDay = await context.UserPlanDayExecutions
            .AsNoTracking()
            .SingleAsync(x => x.EnrollmentId == enrollmentId && x.LocalDate == localDate);
        Assert.Equal("partial", partialDay.Status);

        var second = await service.CompleteExerciseAsync(
            1,
            enrollmentId,
            new CompletePlanExerciseRequest
            {
                LocalDate = localDate,
                PlanDayExerciseId = planDayExercises[1].Id
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, second.ResultType);
        Assert.NotNull(second.Value);
        Assert.Equal("completed", second.Value.Status);

        var finalDay = await context.UserPlanDayExecutions
            .AsNoTracking()
            .Include(x => x.ExerciseExecutions)
            .SingleAsync(x => x.EnrollmentId == enrollmentId && x.LocalDate == localDate);
        Assert.Equal("completed", finalDay.Status);
        Assert.Equal(2, finalDay.ExerciseExecutions.Count);
    }

    [Fact]
    public async Task CompleteExerciseAsync_RejectsWrongDayExercise()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1, 2]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "wrong-day",
            durationWeeks: 1,
            days:
            [
                BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)]),
                BuildPlanDay(1, 2, [BuildPlanExercise(2, 1)])
            ]);
        var enrollmentId = await EnrollUserAsync(service, 1, planId, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

        var day2ExerciseId = await context.PlanDayExercises
            .AsNoTracking()
            .Where(x => x.PlanDay.PlanTemplateId == planId && x.PlanDay.WeekNumber == 1 && x.PlanDay.DayNumber == 2)
            .Select(x => x.Id)
            .SingleAsync();

        var result = await service.CompleteExerciseAsync(
            1,
            enrollmentId,
            new CompletePlanExerciseRequest
            {
                LocalDate = new DateOnly(2026, 10, 1),
                PlanDayExerciseId = day2ExerciseId
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, result.ResultType);
        Assert.Equal(
            "planDayExerciseId does not belong to the scheduled day for this enrollment.",
            result.Error);
    }

    [Fact]
    public async Task CompleteExerciseAsync_RejectsForeignWorkoutEntry()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1, 2], [1]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "foreign-entry",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);
        var enrollmentId = await EnrollUserAsync(service, 1, planId, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));

        var planDayExerciseId = await context.PlanDayExercises
            .AsNoTracking()
            .Where(x => x.PlanDay.PlanTemplateId == planId)
            .Select(x => x.Id)
            .SingleAsync();

        var (_, foreignEntry) = await SeedWorkoutAsync(context, 2, 1);

        var result = await service.CompleteExerciseAsync(
            1,
            enrollmentId,
            new CompletePlanExerciseRequest
            {
                LocalDate = new DateOnly(2026, 11, 1),
                PlanDayExerciseId = planDayExerciseId,
                LinkedWorkoutEntryId = foreignEntry.Id
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.ValidationError, result.ResultType);
        Assert.Equal("linkedWorkoutEntryId must reference one of your workout entries.", result.Error);
    }

    [Fact]
    public async Task CancelEnrollmentAsync_MarksEnrollmentCancelled()
    {
        await using var context = CreateContext();
        await SeedUsersAndExercisesAsync(context, [1], [1]);
        var service = CreateService(context);

        var planId = await CreatePublishedPlanAsync(
            service,
            slug: "cancel-enrollment",
            durationWeeks: 1,
            days: [BuildPlanDay(1, 1, [BuildPlanExercise(1, 1)])]);
        var enrollmentId = await EnrollUserAsync(service, 1, planId, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await service.CancelEnrollmentAsync(1, enrollmentId, CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, result.ResultType);

        var enrollment = await context.UserPlanEnrollments
            .AsNoTracking()
            .SingleAsync(x => x.Id == enrollmentId);
        Assert.Equal("cancelled", enrollment.Status);
    }

    private static PlansService CreateService(WorkoutLogDbContext context) => new(context);

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
            context.Users.Add(
                new AuthUser
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
            context.Exercises.Add(
                new Exercise
                {
                    Id = exerciseId,
                    Name = $"exercise-{exerciseId}",
                    Difficulty = 1
                });
        }

        await context.SaveChangesAsync();
    }

    private static PlanDayRequest BuildPlanDay(
        int weekNumber,
        int dayNumber,
        List<PlanDayExerciseRequest> exercises,
        string? title = null,
        string? notes = null)
    {
        return new PlanDayRequest
        {
            WeekNumber = weekNumber,
            DayNumber = dayNumber,
            Title = title,
            Notes = notes,
            Exercises = exercises
        };
    }

    private static PlanDayExerciseRequest BuildPlanExercise(
        int exerciseId,
        int orderNumber,
        int? sets = null,
        int? repetitions = null)
    {
        return new PlanDayExerciseRequest
        {
            ExerciseId = exerciseId,
            OrderNumber = orderNumber,
            Sets = sets,
            Repetitions = repetitions
        };
    }

    private static async Task<int> CreatePublishedPlanAsync(
        PlansService service,
        string slug,
        int durationWeeks,
        List<PlanDayRequest> days)
    {
        var create = await service.CreatePlanAsync(
            new CreatePlanTemplateRequest
            {
                Slug = slug,
                Name = slug,
                DurationWeeks = durationWeeks,
                Days = days
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, create.ResultType);
        Assert.NotNull(create.Value);

        var update = await service.UpdatePlanAsync(
            create.Value.Id,
            new UpdatePlanTemplateRequest
            {
                Name = create.Value.Name,
                Description = create.Value.Description,
                DurationWeeks = durationWeeks,
                Status = "published",
                Days = days
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, update.ResultType);
        return create.Value.Id;
    }

    private static async Task<int> EnrollUserAsync(
        PlansService service,
        int userId,
        int planId,
        DateTime startedAtUtc)
    {
        var enrollment = await service.EnrollAsync(
            userId,
            planId,
            new EnrollInPlanRequest
            {
                StartedAtUtc = startedAtUtc,
                TimeZoneId = "UTC"
            },
            CancellationToken.None);

        Assert.Equal(PlanOperationResultType.Success, enrollment.ResultType);
        Assert.NotNull(enrollment.Value);
        return enrollment.Value.Id;
    }

    private static async Task<(WorkoutSession Session, WorkoutEntry Entry)> SeedWorkoutAsync(
        WorkoutLogDbContext context,
        int userId,
        int exerciseId)
    {
        var now = DateTime.UtcNow;
        var session = new WorkoutSession
        {
            UserId = userId,
            Mood = 5,
            Feeling = "good",
            DurationInSeconds = 1800,
            Calories = 200,
            TotalKgMoved = 100,
            TotalRepetitions = 20,
            AverageRateOfPerceivedExertion = 6,
            PerformedAtUtc = now,
            CreatedAtUtc = now
        };

        var entry = new WorkoutEntry
        {
            WorkoutSession = session,
            ExerciseId = exerciseId,
            OrderNumber = 1,
            Repetitions = 10,
            Mood = 5,
            WeightUsedKg = 40,
            RateOfPerceivedExertion = 6,
            KcalBurned = 50,
            CreatedAtUtc = now
        };

        context.WorkoutSessions.Add(session);
        context.WorkoutEntries.Add(entry);
        await context.SaveChangesAsync();

        return (session, entry);
    }
}
