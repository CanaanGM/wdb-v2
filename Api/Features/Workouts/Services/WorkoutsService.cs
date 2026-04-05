using System.Linq.Expressions;
using Api.Application.Contracts.Querying;
using Api.Application.Querying;
using Api.Application.Text;
using Api.Features.UserExerciseStats.Services;
using Api.Features.Workouts.Contracts;
using Domain.Workouts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Workouts.Services;

public sealed class WorkoutsService(
    WorkoutLogDbContext dbContext,
    IUserExerciseStatsService userExerciseStatsService) : IWorkoutsService
{
    public async Task<PagedResponse<WorkoutResponse>> SearchAsync(
        int userId,
        SearchWorkoutsRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();

        var fromUtc = request.FromUtc;
        var toUtc = request.ToUtc;
        var exerciseId = request.ExerciseId;
        var minMood = request.MinMood;
        var maxMood = request.MaxMood;

        var query = dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereIf(fromUtc.HasValue, x => x.PerformedAtUtc >= fromUtc!.Value)
            .WhereIf(toUtc.HasValue, x => x.PerformedAtUtc <= toUtc!.Value)
            .WhereIf(exerciseId.HasValue, x => x.Entries.Any(e => e.ExerciseId == exerciseId!.Value))
            .WhereIf(minMood.HasValue, x => x.Mood >= minMood!.Value)
            .WhereIf(maxMood.HasValue, x => x.Mood <= maxMood!.Value)
            .WhereIf(
                !string.IsNullOrWhiteSpace(normalizedSearch),
                x => EF.Functions.ILike(x.Feeling, $"%{normalizedSearch}%")
                     || (x.Notes != null && EF.Functions.ILike(x.Notes, $"%{normalizedSearch}%")))
            .OrderByDescending(x => x.PerformedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(MapToResponseExpression());

        return await query.ToPagedResponseAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<WorkoutResponse?> GetByIdAsync(int userId, int workoutId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == workoutId)
            .Select(MapToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<List<WorkoutResponse>> GetRecentAsync(int userId, int hours, CancellationToken cancellationToken)
    {
        var sinceUtc = DateTime.UtcNow.AddHours(-hours);

        return await dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.PerformedAtUtc >= sinceUtc)
            .OrderByDescending(x => x.PerformedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(MapToResponseExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkoutOperationResult<WorkoutResponse>> CreateAsync(
        int userId,
        CreateWorkoutRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Entries.Count == 0)
        {
            return WorkoutOperationResult<WorkoutResponse>.ValidationError("At least one workout entry is required.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            request.Entries.Select(x => x.ExerciseId),
            cancellationToken);

        if (exerciseValidation.Error is not null)
        {
            return WorkoutOperationResult<WorkoutResponse>.ValidationError(exerciseValidation.Error);
        }

        var performedAtUtc = request.PerformedAtUtc ?? DateTime.UtcNow;
        var sessionMetrics = CalculateSessionMetrics(request.DurationInMinutes, request.Entries);

        var workout = new WorkoutSession
        {
            UserId = userId,
            Feeling = StorageTextNormalizer.NormalizeText(request.Feeling),
            Mood = request.Mood,
            Notes = StorageTextNormalizer.NormalizeOptionalText(request.Notes),
            DurationInSeconds = sessionMetrics.DurationInSeconds,
            Calories = sessionMetrics.Calories,
            TotalKgMoved = sessionMetrics.TotalKgMoved,
            TotalRepetitions = sessionMetrics.TotalRepetitions,
            AverageRateOfPerceivedExertion = sessionMetrics.AverageRateOfPerceivedExertion,
            PerformedAtUtc = performedAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            Entries = MapEntries(request.Entries)
        };

        dbContext.WorkoutSessions.Add(workout);
        await dbContext.SaveChangesAsync(cancellationToken);

        await userExerciseStatsService.RecomputeForExercisesAsync(userId, exerciseValidation.ExerciseIds, cancellationToken);

        var created = await GetByIdAsync(userId, workout.Id, cancellationToken);
        if (created is null)
        {
            throw new InvalidOperationException("Workout creation failed.");
        }

        return WorkoutOperationResult<WorkoutResponse>.Success(created);
    }

    public async Task<WorkoutOperationResult<int>> CreateBulkAsync(
        int userId,
        List<CreateWorkoutRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return WorkoutOperationResult<int>.ValidationError("At least one workout is required.");
        }

        if (requests.Any(x => x.Entries.Count == 0))
        {
            return WorkoutOperationResult<int>.ValidationError("Every workout must have at least one entry.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            requests.SelectMany(x => x.Entries).Select(x => x.ExerciseId),
            cancellationToken);

        if (exerciseValidation.Error is not null)
        {
            return WorkoutOperationResult<int>.ValidationError(exerciseValidation.Error);
        }

        var now = DateTime.UtcNow;

        var workouts = requests
            .Select(request =>
            {
                var performedAtUtc = request.PerformedAtUtc ?? now;
                var sessionMetrics = CalculateSessionMetrics(request.DurationInMinutes, request.Entries);

                return new WorkoutSession
                {
                    UserId = userId,
                    Feeling = StorageTextNormalizer.NormalizeText(request.Feeling),
                    Mood = request.Mood,
                    Notes = StorageTextNormalizer.NormalizeOptionalText(request.Notes),
                    DurationInSeconds = sessionMetrics.DurationInSeconds,
                    Calories = sessionMetrics.Calories,
                    TotalKgMoved = sessionMetrics.TotalKgMoved,
                    TotalRepetitions = sessionMetrics.TotalRepetitions,
                    AverageRateOfPerceivedExertion = sessionMetrics.AverageRateOfPerceivedExertion,
                    PerformedAtUtc = performedAtUtc,
                    CreatedAtUtc = now,
                    Entries = MapEntries(request.Entries)
                };
            })
            .ToList();

        dbContext.WorkoutSessions.AddRange(workouts);
        await dbContext.SaveChangesAsync(cancellationToken);

        await userExerciseStatsService.RecomputeForExercisesAsync(userId, exerciseValidation.ExerciseIds, cancellationToken);

        return WorkoutOperationResult<int>.Success(workouts.Count);
    }

    public async Task<WorkoutOperationResult<WorkoutResponse>> UpdateAsync(
        int userId,
        int workoutId,
        UpdateWorkoutRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Entries.Count == 0)
        {
            return WorkoutOperationResult<WorkoutResponse>.ValidationError("At least one workout entry is required.");
        }

        var workout = await dbContext.WorkoutSessions
            .Include(x => x.Entries)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == workoutId, cancellationToken);

        if (workout is null)
        {
            return WorkoutOperationResult<WorkoutResponse>.NotFound($"Workout with id '{workoutId}' was not found.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            request.Entries.Select(x => x.ExerciseId),
            cancellationToken);

        if (exerciseValidation.Error is not null)
        {
            return WorkoutOperationResult<WorkoutResponse>.ValidationError(exerciseValidation.Error);
        }

        var affectedExerciseIds = workout.Entries
            .Select(x => x.ExerciseId)
            .Union(exerciseValidation.ExerciseIds)
            .Distinct()
            .ToList();

        var sessionMetrics = CalculateSessionMetrics(request.DurationInMinutes, request.Entries);

        workout.Feeling = StorageTextNormalizer.NormalizeText(request.Feeling);
        workout.Mood = request.Mood;
        workout.Notes = StorageTextNormalizer.NormalizeOptionalText(request.Notes);
        workout.PerformedAtUtc = request.PerformedAtUtc ?? workout.PerformedAtUtc;
        workout.DurationInSeconds = sessionMetrics.DurationInSeconds;
        workout.Calories = sessionMetrics.Calories;
        workout.TotalKgMoved = sessionMetrics.TotalKgMoved;
        workout.TotalRepetitions = sessionMetrics.TotalRepetitions;
        workout.AverageRateOfPerceivedExertion = sessionMetrics.AverageRateOfPerceivedExertion;

        dbContext.WorkoutEntries.RemoveRange(workout.Entries);
        workout.Entries = MapEntries(request.Entries);

        await dbContext.SaveChangesAsync(cancellationToken);

        await userExerciseStatsService.RecomputeForExercisesAsync(userId, affectedExerciseIds, cancellationToken);

        var updated = await GetByIdAsync(userId, workout.Id, cancellationToken);
        if (updated is null)
        {
            throw new InvalidOperationException("Workout update failed.");
        }

        return WorkoutOperationResult<WorkoutResponse>.Success(updated);
    }

    public async Task<WorkoutOperationResult> DeleteAsync(int userId, int workoutId, CancellationToken cancellationToken)
    {
        var workout = await dbContext.WorkoutSessions
            .Include(x => x.Entries)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == workoutId, cancellationToken);

        if (workout is null)
        {
            return WorkoutOperationResult.NotFound($"Workout with id '{workoutId}' was not found.");
        }

        var affectedExerciseIds = workout.Entries
            .Select(x => x.ExerciseId)
            .Distinct()
            .ToList();

        dbContext.WorkoutSessions.Remove(workout);
        await dbContext.SaveChangesAsync(cancellationToken);

        await userExerciseStatsService.RecomputeForExercisesAsync(userId, affectedExerciseIds, cancellationToken);

        return WorkoutOperationResult.Success();
    }

    private async Task<(List<int> ExerciseIds, string? Error)> ValidateExerciseIdsAsync(
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

        var existingExerciseIds = await dbContext.Exercises
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var missingExerciseIds = ids
            .Except(existingExerciseIds)
            .OrderBy(x => x)
            .ToList();

        if (missingExerciseIds.Count > 0)
        {
            return ([], $"Exercises not found: {string.Join(", ", missingExerciseIds)}.");
        }

        return (ids, null);
    }

    private static List<WorkoutEntry> MapEntries(IEnumerable<WorkoutEntryRequest> requests)
    {
        var now = DateTime.UtcNow;

        return requests
            .Select(x => new WorkoutEntry
            {
                ExerciseId = x.ExerciseId,
                OrderNumber = x.OrderNumber,
                Repetitions = x.Repetitions,
                Mood = x.Mood,
                TimerInSeconds = x.TimerInSeconds,
                WeightUsedKg = x.WeightUsedKg,
                RateOfPerceivedExertion = x.RateOfPerceivedExertion,
                RestInSeconds = x.RestInSeconds,
                KcalBurned = x.KcalBurned,
                DistanceInMeters = x.DistanceInMeters,
                Notes = StorageTextNormalizer.NormalizeOptionalText(x.Notes),
                Incline = x.Incline,
                Speed = x.Speed,
                HeartRateAvg = x.HeartRateAvg,
                CreatedAtUtc = now
            })
            .ToList();
    }

    private static WorkoutSessionMetrics CalculateSessionMetrics(
        int durationInMinutes,
        IReadOnlyCollection<WorkoutEntryRequest> entries)
    {
        var timerSeconds = entries.Sum(x => x.TimerInSeconds ?? 0);
        var durationFromRequest = checked(durationInMinutes * 60);

        return new WorkoutSessionMetrics
        {
            DurationInSeconds = Math.Max(durationFromRequest, timerSeconds),
            Calories = entries.Sum(x => x.KcalBurned),
            TotalKgMoved = entries.Sum(x => x.WeightUsedKg * x.Repetitions),
            TotalRepetitions = entries.Sum(x => x.Repetitions),
            AverageRateOfPerceivedExertion = entries.Count == 0
                ? 0
                : entries.Average(x => x.RateOfPerceivedExertion)
        };
    }

    private static Expression<Func<WorkoutSession, WorkoutResponse>> MapToResponseExpression()
    {
        return x => new WorkoutResponse
        {
            Id = x.Id,
            UserId = x.UserId,
            Feeling = x.Feeling,
            Mood = x.Mood,
            Notes = x.Notes,
            DurationInMinutes = (double)x.DurationInSeconds / 60d,
            TotalCaloriesBurned = x.Calories,
            TotalKgMoved = x.TotalKgMoved,
            TotalRepetitions = x.TotalRepetitions,
            AverageRateOfPerceivedExertion = x.AverageRateOfPerceivedExertion,
            PerformedAtUtc = x.PerformedAtUtc,
            CreatedAtUtc = x.CreatedAtUtc,
            Entries = x.Entries
                .OrderBy(e => e.OrderNumber)
                .Select(e => new WorkoutEntryResponse
                {
                    Id = e.Id,
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.Exercise.Name,
                    OrderNumber = e.OrderNumber,
                    Repetitions = e.Repetitions,
                    Mood = e.Mood,
                    TimerInSeconds = e.TimerInSeconds,
                    WeightUsedKg = e.WeightUsedKg,
                    RateOfPerceivedExertion = e.RateOfPerceivedExertion,
                    RestInSeconds = e.RestInSeconds,
                    KcalBurned = e.KcalBurned,
                    DistanceInMeters = e.DistanceInMeters,
                    Notes = e.Notes,
                    Incline = e.Incline,
                    Speed = e.Speed,
                    HeartRateAvg = e.HeartRateAvg
                })
                .ToList()
        };
    }

    private sealed class WorkoutSessionMetrics
    {
        public int DurationInSeconds { get; init; }

        public int Calories { get; init; }

        public double TotalKgMoved { get; init; }

        public int TotalRepetitions { get; init; }

        public double AverageRateOfPerceivedExertion { get; init; }
    }
}
