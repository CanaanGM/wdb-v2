using Api.Application.Contracts.Querying;
using Api.Application.Querying;
using Api.Application.Text;
using Api.Features.Plans.Contracts;
using Domain.Plans;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Plans.Services;

public sealed class PlansService(WorkoutLogDbContext dbContext) : IPlansService
{
    public async Task<PagedResponse<PlanTemplateResponse>> SearchPlansAsync(
        SearchPlansRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();
        var status = NormalizeStatus(request.Status);

        var query = dbContext.PlanTemplates
            .AsNoTracking()
            .WhereIf(status is not null, x => x.Status == status)
            .WhereIf(
                normalizedSearch is not null,
                x => EF.Functions.ILike(x.Name, $"%{normalizedSearch}%")
                     || EF.Functions.ILike(x.Slug, $"%{normalizedSearch}%")
                     || (x.Description != null && EF.Functions.ILike(x.Description, $"%{normalizedSearch}%")))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new PlanTemplateResponse
            {
                Id = x.Id,
                Slug = x.Slug,
                Name = x.Name,
                Description = x.Description,
                DurationWeeks = x.DurationWeeks,
                Status = x.Status,
                Version = x.Version,
                CreatedAtUtc = x.CreatedAtUtc
            });

        return await query.ToPagedResponseAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<PlanTemplateDetailsResponse?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await dbContext.PlanTemplates
            .AsNoTracking()
            .Include(x => x.Days)
            .ThenInclude(x => x.Exercises)
            .ThenInclude(x => x.Exercise)
            .ThenInclude(x => x.ExerciseTrainingTypes)
            .ThenInclude(x => x.TrainingType)
            .Include(x => x.Days)
            .ThenInclude(x => x.Exercises)
            .ThenInclude(x => x.Exercise)
            .ThenInclude(x => x.ExerciseEquipments)
            .ThenInclude(x => x.Equipment)
            .SingleOrDefaultAsync(x => x.Id == planId, cancellationToken);

        return plan is null
            ? null
            : MapPlanDetails(plan);
    }

    public async Task<PlanOperationResult<PlanTemplateDetailsResponse>> CreatePlanAsync(
        CreatePlanTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Days.Any(x => x.WeekNumber > request.DurationWeeks))
        {
            return PlanOperationResult<PlanTemplateDetailsResponse>.ValidationError(
                "Plan day week number cannot exceed durationWeeks.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            request.Days.SelectMany(x => x.Exercises).Select(x => x.ExerciseId),
            cancellationToken);
        if (exerciseValidation.Error is not null)
        {
            return PlanOperationResult<PlanTemplateDetailsResponse>.ValidationError(exerciseValidation.Error);
        }

        var normalizedSlug = StorageTextNormalizer.NormalizeKey(request.Slug);
        var nextVersion = await dbContext.PlanTemplates
            .AsNoTracking()
            .Where(x => x.Slug == normalizedSlug)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;
        var plan = new PlanTemplate
        {
            Slug = normalizedSlug,
            Name = request.Name.Trim(),
            Description = StorageTextNormalizer.NormalizeOptionalText(request.Description),
            DurationWeeks = request.DurationWeeks,
            Status = PlanStatuses.Draft,
            Version = nextVersion + 1,
            CreatedAtUtc = now,
            Days = MapPlanDays(request.Days, now)
        };

        dbContext.PlanTemplates.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await GetPlanByIdAsync(plan.Id, cancellationToken);
        if (created is null)
        {
            throw new InvalidOperationException("Plan creation failed.");
        }

        return PlanOperationResult<PlanTemplateDetailsResponse>.Success(created);
    }

    public async Task<PlanOperationResult<int>> CreatePlansBulkAsync(
        IReadOnlyList<CreatePlanTemplateRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return PlanOperationResult<int>.ValidationError("At least one plan is required.");
        }

        if (requests.Any(x => x.Days.Any(day => day.WeekNumber > x.DurationWeeks)))
        {
            return PlanOperationResult<int>.ValidationError(
                "Plan day week number cannot exceed durationWeeks.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            requests
                .SelectMany(x => x.Days)
                .SelectMany(x => x.Exercises)
                .Select(x => x.ExerciseId),
            cancellationToken);
        if (exerciseValidation.Error is not null)
        {
            return PlanOperationResult<int>.ValidationError(exerciseValidation.Error);
        }

        var normalizedSlugs = requests
            .Select(x => StorageTextNormalizer.NormalizeKey(x.Slug))
            .Distinct()
            .ToList();

        var maxVersionBySlug = await dbContext.PlanTemplates
            .AsNoTracking()
            .Where(x => normalizedSlugs.Contains(x.Slug))
            .GroupBy(x => x.Slug)
            .Select(x => new { x.Key, MaxVersion = x.Max(p => p.Version) })
            .ToDictionaryAsync(x => x.Key, x => x.MaxVersion, cancellationToken);

        var now = DateTime.UtcNow;
        var plans = new List<PlanTemplate>(requests.Count);
        foreach (var request in requests)
        {
            var normalizedSlug = StorageTextNormalizer.NormalizeKey(request.Slug);
            var nextVersion = maxVersionBySlug.GetValueOrDefault(normalizedSlug, 0) + 1;
            maxVersionBySlug[normalizedSlug] = nextVersion;

            plans.Add(new PlanTemplate
            {
                Slug = normalizedSlug,
                Name = request.Name.Trim(),
                Description = StorageTextNormalizer.NormalizeOptionalText(request.Description),
                DurationWeeks = request.DurationWeeks,
                Status = PlanStatuses.Draft,
                Version = nextVersion,
                CreatedAtUtc = now,
                Days = MapPlanDays(request.Days, now)
            });
        }

        dbContext.PlanTemplates.AddRange(plans);
        await dbContext.SaveChangesAsync(cancellationToken);
        return PlanOperationResult<int>.Success(plans.Count);
    }

    public async Task<PlanOperationResult<PlanTemplateDetailsResponse>> UpdatePlanAsync(
        int planId,
        UpdatePlanTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Days.Any(x => x.WeekNumber > request.DurationWeeks))
        {
            return PlanOperationResult<PlanTemplateDetailsResponse>.ValidationError(
                "Plan day week number cannot exceed durationWeeks.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            request.Days.SelectMany(x => x.Exercises).Select(x => x.ExerciseId),
            cancellationToken);
        if (exerciseValidation.Error is not null)
        {
            return PlanOperationResult<PlanTemplateDetailsResponse>.ValidationError(exerciseValidation.Error);
        }

        var plan = await dbContext.PlanTemplates
            .Include(x => x.Days)
            .ThenInclude(x => x.Exercises)
            .SingleOrDefaultAsync(x => x.Id == planId, cancellationToken);

        if (plan is null)
        {
            return PlanOperationResult<PlanTemplateDetailsResponse>.NotFound($"Plan with id '{planId}' was not found.");
        }

        var normalizedStatus = NormalizeStatus(request.Status);
        if (normalizedStatus is null)
        {
            return PlanOperationResult<PlanTemplateDetailsResponse>.ValidationError("Unsupported plan status.");
        }

        plan.Name = request.Name.Trim();
        plan.Description = StorageTextNormalizer.NormalizeOptionalText(request.Description);
        plan.DurationWeeks = request.DurationWeeks;
        plan.Status = normalizedStatus;

        dbContext.PlanDayExercises.RemoveRange(plan.Days.SelectMany(x => x.Exercises));
        dbContext.PlanDays.RemoveRange(plan.Days);
        plan.Days = MapPlanDays(request.Days, DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await GetPlanByIdAsync(plan.Id, cancellationToken);
        if (updated is null)
        {
            throw new InvalidOperationException("Plan update failed.");
        }

        return PlanOperationResult<PlanTemplateDetailsResponse>.Success(updated);
    }

    public async Task<PlanOperationResult<int>> CreatePlanDayExercisesBulkAsync(
        int planId,
        int weekNumber,
        int dayNumber,
        IReadOnlyList<PlanDayExerciseRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return PlanOperationResult<int>.ValidationError("At least one exercise is required.");
        }

        var duplicateOrderNumbers = requests
            .GroupBy(x => x.OrderNumber)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();
        if (duplicateOrderNumbers.Count > 0)
        {
            return PlanOperationResult<int>.ValidationError(
                $"Duplicate exercise order numbers: {string.Join(", ", duplicateOrderNumbers)}.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            requests.Select(x => x.ExerciseId),
            cancellationToken);
        if (exerciseValidation.Error is not null)
        {
            return PlanOperationResult<int>.ValidationError(exerciseValidation.Error);
        }

        var planDay = await dbContext.PlanDays
            .Include(x => x.Exercises)
            .SingleOrDefaultAsync(
                x => x.PlanTemplateId == planId
                     && x.WeekNumber == weekNumber
                     && x.DayNumber == dayNumber,
                cancellationToken);
        if (planDay is null)
        {
            return PlanOperationResult<int>.NotFound(
                $"Plan day was not found for planId '{planId}', weekNumber '{weekNumber}', dayNumber '{dayNumber}'.");
        }

        var conflictingOrderNumbers = requests
            .Select(x => x.OrderNumber)
            .Intersect(planDay.Exercises.Select(x => x.OrderNumber))
            .OrderBy(x => x)
            .ToList();
        if (conflictingOrderNumbers.Count > 0)
        {
            return PlanOperationResult<int>.ValidationError(
                $"Order numbers already exist in plan day: {string.Join(", ", conflictingOrderNumbers)}.");
        }

        var now = DateTime.UtcNow;
        var newExercises = requests
            .OrderBy(x => x.OrderNumber)
            .Select(x => new PlanDayExercise
            {
                PlanDayId = planDay.Id,
                ExerciseId = x.ExerciseId,
                OrderNumber = x.OrderNumber,
                Sets = x.Sets,
                Repetitions = x.Repetitions,
                TargetRateOfPerceivedExertion = x.TargetRateOfPerceivedExertion,
                TargetWeightKg = x.TargetWeightKg,
                TimerInSeconds = x.TimerInSeconds,
                DistanceInMeters = x.DistanceInMeters,
                RestInSeconds = x.RestInSeconds,
                Notes = StorageTextNormalizer.NormalizeOptionalText(x.Notes),
                CreatedAtUtc = now
            })
            .ToList();

        dbContext.PlanDayExercises.AddRange(newExercises);
        await dbContext.SaveChangesAsync(cancellationToken);
        return PlanOperationResult<int>.Success(newExercises.Count);
    }

    public async Task<PlanOperationResult<UserPlanEnrollmentResponse>> EnrollAsync(
        int userId,
        int planId,
        EnrollInPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.PlanTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == planId, cancellationToken);
        if (plan is null)
        {
            return PlanOperationResult<UserPlanEnrollmentResponse>.NotFound($"Plan with id '{planId}' was not found.");
        }

        if (plan.Status != PlanStatuses.Published)
        {
            return PlanOperationResult<UserPlanEnrollmentResponse>.ValidationError("Only published plans can be enrolled.");
        }

        var hasActiveEnrollment = await dbContext.UserPlanEnrollments
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId
                     && x.PlanTemplateId == planId
                     && x.Status == EnrollmentStatuses.Active,
                cancellationToken);
        if (hasActiveEnrollment)
        {
            return PlanOperationResult<UserPlanEnrollmentResponse>.ValidationError(
                "You already have an active enrollment for this plan.");
        }

        var (timeZoneInfo, timeZoneError) = ResolveTimeZone(request.TimeZoneId);
        if (timeZoneError is not null)
        {
            return PlanOperationResult<UserPlanEnrollmentResponse>.ValidationError(timeZoneError);
        }
        if (timeZoneInfo is null)
        {
            return PlanOperationResult<UserPlanEnrollmentResponse>.ValidationError("Timezone resolution failed.");
        }

        var startedAtUtc = request.StartedAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var startLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startedAtUtc, timeZoneInfo));
        var endLocalDateInclusive = startLocalDate.AddDays(plan.DurationWeeks * 7 - 1);
        var maxDisplayOrder = await dbContext.UserPlanEnrollments
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var enrollment = new UserPlanEnrollment
        {
            UserId = userId,
            PlanTemplateId = planId,
            StartedAtUtc = startedAtUtc,
            TimeZoneId = timeZoneInfo.Id,
            StartLocalDate = startLocalDate,
            EndLocalDateInclusive = endLocalDateInclusive,
            Status = EnrollmentStatuses.Active,
            DisplayOrder = maxDisplayOrder + 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.UserPlanEnrollments.Add(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.UserPlanEnrollments
            .AsNoTracking()
            .Include(x => x.PlanTemplate)
            .SingleAsync(x => x.Id == enrollment.Id, cancellationToken);

        return PlanOperationResult<UserPlanEnrollmentResponse>.Success(MapEnrollment(created));
    }

    public async Task<PagedResponse<UserPlanEnrollmentResponse>> SearchEnrollmentsAsync(
        int userId,
        SearchUserPlanEnrollmentsRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();
        var status = NormalizeEnrollmentStatus(request.Status);

        var query = dbContext.UserPlanEnrollments
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereIf(status is not null, x => x.Status == status)
            .WhereIf(
                normalizedSearch is not null,
                x => EF.Functions.ILike(x.PlanTemplate.Name, $"%{normalizedSearch}%")
                     || EF.Functions.ILike(x.PlanTemplate.Slug, $"%{normalizedSearch}%"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new UserPlanEnrollmentResponse
            {
                Id = x.Id,
                PlanTemplateId = x.PlanTemplateId,
                PlanName = x.PlanTemplate.Name,
                PlanSlug = x.PlanTemplate.Slug,
                PlanVersion = x.PlanTemplate.Version,
                StartedAtUtc = x.StartedAtUtc,
                TimeZoneId = x.TimeZoneId,
                StartLocalDate = x.StartLocalDate,
                EndLocalDateInclusive = x.EndLocalDateInclusive,
                Status = x.Status,
                DisplayOrder = x.DisplayOrder,
                CreatedAtUtc = x.CreatedAtUtc
            });

        return await query.ToPagedResponseAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<List<UserPlanAgendaDayResponse>> GetAgendaAsync(
        int userId,
        GetUserPlanAgendaRequest request,
        CancellationToken cancellationToken)
    {
        var enrollments = await dbContext.UserPlanEnrollments
            .AsNoTracking()
            .Include(x => x.PlanTemplate)
            .ThenInclude(x => x.Days)
            .ThenInclude(x => x.Exercises)
            .ThenInclude(x => x.Exercise)
            .Where(x => x.UserId == userId && x.Status == EnrollmentStatuses.Active)
            .Where(x => x.EndLocalDateInclusive >= request.FromLocalDate && x.StartLocalDate <= request.ToLocalDate)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            return [];
        }

        var enrollmentIds = enrollments.Select(x => x.Id).ToList();
        var dayExecutions = await dbContext.UserPlanDayExecutions
            .AsNoTracking()
            .Include(x => x.ExerciseExecutions)
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .Where(x => x.LocalDate >= request.FromLocalDate && x.LocalDate <= request.ToLocalDate)
            .ToListAsync(cancellationToken);

        var dayExecutionByKey = dayExecutions.ToDictionary(x => (x.EnrollmentId, x.LocalDate));
        var result = new List<UserPlanAgendaDayResponse>();

        foreach (var enrollment in enrollments)
        {
            var from = Max(request.FromLocalDate, enrollment.StartLocalDate);
            var to = Min(request.ToLocalDate, enrollment.EndLocalDateInclusive);
            for (var current = from; current <= to; current = current.AddDays(1))
            {
                var offset = current.DayNumber - enrollment.StartLocalDate.DayNumber;
                if (offset < 0)
                {
                    continue;
                }

                var weekNumber = offset / 7 + 1;
                var dayNumber = offset % 7 + 1;
                var planDay = enrollment.PlanTemplate.Days
                    .SingleOrDefault(x => x.WeekNumber == weekNumber && x.DayNumber == dayNumber);

                dayExecutionByKey.TryGetValue((enrollment.Id, current), out var dayExecution);

                if (planDay is null && dayExecution is null)
                {
                    continue;
                }

                var agendaExercises = planDay is null
                    ? []
                    : planDay.Exercises
                        .OrderBy(x => x.OrderNumber)
                        .Select(x =>
                        {
                            var status = dayExecution?.ExerciseExecutions
                                .Where(e => e.PlanDayExerciseId == x.Id)
                                .OrderByDescending(e => e.UpdatedAtUtc)
                                .Select(e => e.Status)
                                .FirstOrDefault() ?? ExerciseExecutionStatuses.Pending;

                            return new UserPlanAgendaExerciseResponse
                            {
                                PlanDayExerciseId = x.Id,
                                ExerciseId = x.ExerciseId,
                                ExerciseName = x.Exercise.Name,
                                OrderNumber = x.OrderNumber,
                                Sets = x.Sets,
                                Repetitions = x.Repetitions,
                                TargetRateOfPerceivedExertion = x.TargetRateOfPerceivedExertion,
                                TargetWeightKg = x.TargetWeightKg,
                                TimerInSeconds = x.TimerInSeconds,
                                DistanceInMeters = x.DistanceInMeters,
                                RestInSeconds = x.RestInSeconds,
                                Notes = x.Notes,
                                Status = status
                            };
                        })
                        .ToList();

                result.Add(new UserPlanAgendaDayResponse
                {
                    EnrollmentId = enrollment.Id,
                    PlanTemplateId = enrollment.PlanTemplateId,
                    PlanName = enrollment.PlanTemplate.Name,
                    PlanSlug = enrollment.PlanTemplate.Slug,
                    PlanVersion = enrollment.PlanTemplate.Version,
                    LocalDate = current,
                    WeekNumber = weekNumber,
                    DayNumber = dayNumber,
                    DayTitle = planDay?.Title,
                    Status = dayExecution?.Status ?? DayExecutionStatuses.Scheduled,
                    IsRestDay = planDay is null,
                    Exercises = agendaExercises
                });
            }
        }

        return result
            .OrderBy(x => x.LocalDate)
            .ThenBy(x => x.EnrollmentId)
            .ThenBy(x => x.WeekNumber)
            .ThenBy(x => x.DayNumber)
            .ToList();
    }

    public async Task<PlanOperationResult<UserPlanDayExecutionResponse>> CompleteDayAsync(
        int userId,
        int enrollmentId,
        CompletePlanDayRequest request,
        CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.UserPlanEnrollments
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == enrollmentId, cancellationToken);
        if (enrollment is null)
        {
            return PlanOperationResult<UserPlanDayExecutionResponse>.NotFound(
                $"Enrollment with id '{enrollmentId}' was not found.");
        }

        if (enrollment.Status != EnrollmentStatuses.Active)
        {
            return PlanOperationResult<UserPlanDayExecutionResponse>.ValidationError(
                "Only active enrollments can be updated.");
        }

        if (!IsWithinEnrollmentRange(enrollment, request.LocalDate))
        {
            return PlanOperationResult<UserPlanDayExecutionResponse>.ValidationError(
                "Provided localDate is outside the enrollment date range.");
        }

        if (request.LinkedWorkoutSessionId is not null)
        {
            var isOwnedWorkout = await dbContext.WorkoutSessions
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == request.LinkedWorkoutSessionId.Value && x.UserId == userId,
                    cancellationToken);
            if (!isOwnedWorkout)
            {
                return PlanOperationResult<UserPlanDayExecutionResponse>.ValidationError(
                    "linkedWorkoutSessionId must reference one of your workouts.");
            }
        }

        var dayExecution = await dbContext.UserPlanDayExecutions
            .SingleOrDefaultAsync(
                x => x.EnrollmentId == enrollmentId && x.LocalDate == request.LocalDate,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (dayExecution is null)
        {
            dayExecution = new UserPlanDayExecution
            {
                EnrollmentId = enrollmentId,
                LocalDate = request.LocalDate,
                Status = DayExecutionStatuses.Completed,
                LinkedWorkoutSessionId = request.LinkedWorkoutSessionId,
                Notes = StorageTextNormalizer.NormalizeOptionalText(request.Notes),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.UserPlanDayExecutions.Add(dayExecution);
        }
        else
        {
            dayExecution.Status = DayExecutionStatuses.Completed;
            dayExecution.LinkedWorkoutSessionId = request.LinkedWorkoutSessionId;
            dayExecution.Notes = StorageTextNormalizer.NormalizeOptionalText(request.Notes);
            dayExecution.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return PlanOperationResult<UserPlanDayExecutionResponse>.Success(
            new UserPlanDayExecutionResponse
            {
                EnrollmentId = enrollmentId,
                LocalDate = dayExecution.LocalDate,
                Status = dayExecution.Status,
                LinkedWorkoutSessionId = dayExecution.LinkedWorkoutSessionId,
                Notes = dayExecution.Notes,
                UpdatedAtUtc = dayExecution.UpdatedAtUtc
            });
    }

    public async Task<PlanOperationResult<UserPlanDayExecutionResponse>> SkipDayAsync(
        int userId,
        int enrollmentId,
        SkipPlanDayRequest request,
        CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.UserPlanEnrollments
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == enrollmentId, cancellationToken);
        if (enrollment is null)
        {
            return PlanOperationResult<UserPlanDayExecutionResponse>.NotFound(
                $"Enrollment with id '{enrollmentId}' was not found.");
        }

        if (enrollment.Status != EnrollmentStatuses.Active)
        {
            return PlanOperationResult<UserPlanDayExecutionResponse>.ValidationError(
                "Only active enrollments can be updated.");
        }

        if (!IsWithinEnrollmentRange(enrollment, request.LocalDate))
        {
            return PlanOperationResult<UserPlanDayExecutionResponse>.ValidationError(
                "Provided localDate is outside the enrollment date range.");
        }

        var dayExecution = await dbContext.UserPlanDayExecutions
            .SingleOrDefaultAsync(
                x => x.EnrollmentId == enrollmentId && x.LocalDate == request.LocalDate,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (dayExecution is null)
        {
            dayExecution = new UserPlanDayExecution
            {
                EnrollmentId = enrollmentId,
                LocalDate = request.LocalDate,
                Status = DayExecutionStatuses.Skipped,
                LinkedWorkoutSessionId = null,
                Notes = StorageTextNormalizer.NormalizeOptionalText(request.Notes),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.UserPlanDayExecutions.Add(dayExecution);
        }
        else
        {
            dayExecution.Status = DayExecutionStatuses.Skipped;
            dayExecution.LinkedWorkoutSessionId = null;
            dayExecution.Notes = StorageTextNormalizer.NormalizeOptionalText(request.Notes);
            dayExecution.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return PlanOperationResult<UserPlanDayExecutionResponse>.Success(
            new UserPlanDayExecutionResponse
            {
                EnrollmentId = enrollmentId,
                LocalDate = dayExecution.LocalDate,
                Status = dayExecution.Status,
                LinkedWorkoutSessionId = dayExecution.LinkedWorkoutSessionId,
                Notes = dayExecution.Notes,
                UpdatedAtUtc = dayExecution.UpdatedAtUtc
            });
    }

    public async Task<PlanOperationResult<UserPlanExerciseExecutionResponse>> CompleteExerciseAsync(
        int userId,
        int enrollmentId,
        CompletePlanExerciseRequest request,
        CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.UserPlanEnrollments
            .Include(x => x.PlanTemplate)
            .ThenInclude(x => x.Days)
            .ThenInclude(x => x.Exercises)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == enrollmentId, cancellationToken);
        if (enrollment is null)
        {
            return PlanOperationResult<UserPlanExerciseExecutionResponse>.NotFound(
                $"Enrollment with id '{enrollmentId}' was not found.");
        }

        if (enrollment.Status != EnrollmentStatuses.Active)
        {
            return PlanOperationResult<UserPlanExerciseExecutionResponse>.ValidationError(
                "Only active enrollments can be updated.");
        }

        if (!IsWithinEnrollmentRange(enrollment, request.LocalDate))
        {
            return PlanOperationResult<UserPlanExerciseExecutionResponse>.ValidationError(
                "Provided localDate is outside the enrollment date range.");
        }

        if (request.LinkedWorkoutEntryId is not null)
        {
            var isOwnedWorkoutEntry = await dbContext.WorkoutEntries
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == request.LinkedWorkoutEntryId.Value
                         && x.WorkoutSession.UserId == userId,
                    cancellationToken);
            if (!isOwnedWorkoutEntry)
            {
                return PlanOperationResult<UserPlanExerciseExecutionResponse>.ValidationError(
                    "linkedWorkoutEntryId must reference one of your workout entries.");
            }
        }

        var (weekNumber, dayNumber) = GetWeekAndDay(enrollment.StartLocalDate, request.LocalDate);
        var planDay = enrollment.PlanTemplate.Days
            .SingleOrDefault(x => x.WeekNumber == weekNumber && x.DayNumber == dayNumber);
        if (planDay is null)
        {
            return PlanOperationResult<UserPlanExerciseExecutionResponse>.ValidationError(
                "No plan day exists for the provided localDate.");
        }

        var planDayExercise = planDay.Exercises.SingleOrDefault(x => x.Id == request.PlanDayExerciseId);
        if (planDayExercise is null)
        {
            return PlanOperationResult<UserPlanExerciseExecutionResponse>.ValidationError(
                "planDayExerciseId does not belong to the scheduled day for this enrollment.");
        }

        var now = DateTime.UtcNow;
        var dayExecution = await dbContext.UserPlanDayExecutions
            .Include(x => x.ExerciseExecutions)
            .SingleOrDefaultAsync(
                x => x.EnrollmentId == enrollmentId && x.LocalDate == request.LocalDate,
                cancellationToken);
        if (dayExecution is null)
        {
            dayExecution = new UserPlanDayExecution
            {
                EnrollmentId = enrollmentId,
                LocalDate = request.LocalDate,
                Status = DayExecutionStatuses.Scheduled,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.UserPlanDayExecutions.Add(dayExecution);
        }

        var exerciseExecution = dayExecution.ExerciseExecutions
            .SingleOrDefault(x => x.PlanDayExerciseId == request.PlanDayExerciseId);
        if (exerciseExecution is null)
        {
            exerciseExecution = new UserPlanExerciseExecution
            {
                DayExecution = dayExecution,
                PlanDayExerciseId = request.PlanDayExerciseId,
                Status = ExerciseExecutionStatuses.Completed,
                LinkedWorkoutEntryId = request.LinkedWorkoutEntryId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dayExecution.ExerciseExecutions.Add(exerciseExecution);
        }
        else
        {
            exerciseExecution.Status = ExerciseExecutionStatuses.Completed;
            exerciseExecution.LinkedWorkoutEntryId = request.LinkedWorkoutEntryId;
            exerciseExecution.UpdatedAtUtc = now;
        }

        var completedExerciseIds = dayExecution.ExerciseExecutions
            .Where(x => x.Status == ExerciseExecutionStatuses.Completed)
            .Select(x => x.PlanDayExerciseId)
            .ToHashSet();
        dayExecution.Status = planDay.Exercises.All(x => completedExerciseIds.Contains(x.Id))
            ? DayExecutionStatuses.Completed
            : DayExecutionStatuses.Partial;
        dayExecution.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return PlanOperationResult<UserPlanExerciseExecutionResponse>.Success(
            new UserPlanExerciseExecutionResponse
            {
                EnrollmentId = enrollmentId,
                LocalDate = request.LocalDate,
                PlanDayExerciseId = request.PlanDayExerciseId,
                Status = exerciseExecution.Status,
                LinkedWorkoutEntryId = exerciseExecution.LinkedWorkoutEntryId,
                UpdatedAtUtc = exerciseExecution.UpdatedAtUtc
            });
    }

    public async Task<PlanOperationResult> CancelEnrollmentAsync(int userId, int enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.UserPlanEnrollments
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == enrollmentId, cancellationToken);
        if (enrollment is null)
        {
            return PlanOperationResult.NotFound($"Enrollment with id '{enrollmentId}' was not found.");
        }

        enrollment.Status = EnrollmentStatuses.Cancelled;
        await dbContext.SaveChangesAsync(cancellationToken);
        return PlanOperationResult.Success();
    }

    private async Task<(HashSet<int> ExerciseIds, string? Error)> ValidateExerciseIdsAsync(
        IEnumerable<int> exerciseIds,
        CancellationToken cancellationToken)
    {
        var ids = exerciseIds
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return ([], "At least one exercise is required.");
        }

        var existingIds = await dbContext.Exercises
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var missing = ids
            .Except(existingIds)
            .OrderBy(x => x)
            .ToList();
        if (missing.Count > 0)
        {
            return ([], $"Exercises not found: {string.Join(", ", missing)}.");
        }

        return (existingIds.ToHashSet(), null);
    }

    private static List<PlanDay> MapPlanDays(List<PlanDayRequest> requests, DateTime nowUtc)
    {
        return requests
            .OrderBy(x => x.WeekNumber)
            .ThenBy(x => x.DayNumber)
            .Select(day => new PlanDay
            {
                WeekNumber = day.WeekNumber,
                DayNumber = day.DayNumber,
                Title = StorageTextNormalizer.NormalizeOptionalText(day.Title),
                Notes = StorageTextNormalizer.NormalizeOptionalText(day.Notes),
                Exercises = day.Exercises
                    .OrderBy(x => x.OrderNumber)
                    .Select(exercise => new PlanDayExercise
                    {
                        ExerciseId = exercise.ExerciseId,
                        OrderNumber = exercise.OrderNumber,
                        Sets = exercise.Sets,
                        Repetitions = exercise.Repetitions,
                        TargetRateOfPerceivedExertion = exercise.TargetRateOfPerceivedExertion,
                        TargetWeightKg = exercise.TargetWeightKg,
                        TimerInSeconds = exercise.TimerInSeconds,
                        DistanceInMeters = exercise.DistanceInMeters,
                        RestInSeconds = exercise.RestInSeconds,
                        Notes = StorageTextNormalizer.NormalizeOptionalText(exercise.Notes),
                        CreatedAtUtc = nowUtc
                    })
                    .ToList()
            })
            .ToList();
    }

    private static PlanTemplateDetailsResponse MapPlanDetails(PlanTemplate plan)
    {
        var allExercises = plan.Days
            .SelectMany(x => x.Exercises)
            .Select(x => x.Exercise)
            .ToList();

        return new PlanTemplateDetailsResponse
        {
            Id = plan.Id,
            Slug = plan.Slug,
            Name = plan.Name,
            Description = plan.Description,
            DurationWeeks = plan.DurationWeeks,
            Status = plan.Status,
            Version = plan.Version,
            CreatedAtUtc = plan.CreatedAtUtc,
            TrainingTypes = allExercises
                .SelectMany(x => x.ExerciseTrainingTypes)
                .Select(x => x.TrainingType.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToList(),
            RequiredEquipment = allExercises
                .SelectMany(x => x.ExerciseEquipments)
                .Select(x => x.Equipment.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToList(),
            Days = plan.Days
                .OrderBy(x => x.WeekNumber)
                .ThenBy(x => x.DayNumber)
                .Select(day => new PlanDayResponse
                {
                    Id = day.Id,
                    WeekNumber = day.WeekNumber,
                    DayNumber = day.DayNumber,
                    Title = day.Title,
                    Notes = day.Notes,
                    Exercises = day.Exercises
                        .OrderBy(x => x.OrderNumber)
                        .Select(exercise => new PlanDayExerciseResponse
                        {
                            Id = exercise.Id,
                            ExerciseId = exercise.ExerciseId,
                            ExerciseName = exercise.Exercise.Name,
                            OrderNumber = exercise.OrderNumber,
                            Sets = exercise.Sets,
                            Repetitions = exercise.Repetitions,
                            TargetRateOfPerceivedExertion = exercise.TargetRateOfPerceivedExertion,
                            TargetWeightKg = exercise.TargetWeightKg,
                            TimerInSeconds = exercise.TimerInSeconds,
                            DistanceInMeters = exercise.DistanceInMeters,
                            RestInSeconds = exercise.RestInSeconds,
                            Notes = exercise.Notes
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static UserPlanEnrollmentResponse MapEnrollment(UserPlanEnrollment enrollment)
    {
        return new UserPlanEnrollmentResponse
        {
            Id = enrollment.Id,
            PlanTemplateId = enrollment.PlanTemplateId,
            PlanName = enrollment.PlanTemplate.Name,
            PlanSlug = enrollment.PlanTemplate.Slug,
            PlanVersion = enrollment.PlanTemplate.Version,
            StartedAtUtc = enrollment.StartedAtUtc,
            TimeZoneId = enrollment.TimeZoneId,
            StartLocalDate = enrollment.StartLocalDate,
            EndLocalDateInclusive = enrollment.EndLocalDateInclusive,
            Status = enrollment.Status,
            DisplayOrder = enrollment.DisplayOrder,
            CreatedAtUtc = enrollment.CreatedAtUtc
        };
    }

    private static (TimeZoneInfo? TimeZoneInfo, string? Error) ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return (TimeZoneInfo.Utc, null);
        }

        try
        {
            return (TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim()), null);
        }
        catch (TimeZoneNotFoundException)
        {
            return (null, $"Unknown timezone '{timeZoneId}'.");
        }
        catch (InvalidTimeZoneException)
        {
            return (null, $"Invalid timezone '{timeZoneId}'.");
        }
    }

    private static bool IsWithinEnrollmentRange(UserPlanEnrollment enrollment, DateOnly localDate)
    {
        return localDate >= enrollment.StartLocalDate && localDate <= enrollment.EndLocalDateInclusive;
    }

    private static (int WeekNumber, int DayNumber) GetWeekAndDay(DateOnly startLocalDate, DateOnly targetDate)
    {
        var offset = targetDate.DayNumber - startLocalDate.DayNumber;
        var weekNumber = offset / 7 + 1;
        var dayNumber = offset % 7 + 1;
        return (weekNumber, dayNumber);
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = StorageTextNormalizer.NormalizeKey(status);
        return normalized is PlanStatuses.Draft or PlanStatuses.Published or PlanStatuses.Archived
            ? normalized
            : null;
    }

    private static string? NormalizeEnrollmentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = StorageTextNormalizer.NormalizeKey(status);
        return normalized is EnrollmentStatuses.Active or EnrollmentStatuses.Completed or EnrollmentStatuses.Cancelled
            ? normalized
            : null;
    }

    private static DateOnly Max(DateOnly left, DateOnly right) => left >= right ? left : right;

    private static DateOnly Min(DateOnly left, DateOnly right) => left <= right ? left : right;

    private static class PlanStatuses
    {
        public const string Draft = "draft";
        public const string Published = "published";
        public const string Archived = "archived";
    }

    private static class EnrollmentStatuses
    {
        public const string Active = "active";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
    }

    private static class DayExecutionStatuses
    {
        public const string Scheduled = "scheduled";
        public const string Completed = "completed";
        public const string Skipped = "skipped";
        public const string Partial = "partial";
    }

    private static class ExerciseExecutionStatuses
    {
        public const string Pending = "pending";
        public const string Completed = "completed";
    }
}
