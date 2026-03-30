using System.Linq.Expressions;
using Api.Application.Contracts.Querying;
using Api.Application.Querying;
using Api.Features.UserExerciseStats.Contracts;
using Domain.Workouts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.UserExerciseStats.Services;

public sealed class UserExerciseStatsService(WorkoutLogDbContext dbContext) : IUserExerciseStatsService
{
    public async Task<PagedResponse<UserExerciseStatResponse>> SearchAsync(
        int userId,
        SearchUserExerciseStatsRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();

        var exerciseId = request.ExerciseId;
        var exerciseIdValue = exerciseId ?? default;

        var query = dbContext.UserExerciseStats
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereIf(exerciseId.HasValue, x => x.ExerciseId == exerciseIdValue)
            .WhereIf(
                !string.IsNullOrWhiteSpace(normalizedSearch),
                x => EF.Functions.ILike(x.Exercise.Name, $"%{normalizedSearch}%"))
            .OrderBy(x => x.Exercise.Name)
            .Select(MapToResponseExpression());

        return await query.ToPagedResponseAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<UserExerciseStatResponse?> GetByExerciseIdAsync(
        int userId,
        int exerciseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserExerciseStats
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ExerciseId == exerciseId)
            .Select(MapToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task RecomputeForExercisesAsync(
        int userId,
        IReadOnlyCollection<int> exerciseIds,
        CancellationToken cancellationToken)
    {
        if (exerciseIds.Count == 0)
        {
            return;
        }

        var distinctExerciseIds = exerciseIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (distinctExerciseIds.Count == 0)
        {
            return;
        }

        var metrics = await dbContext.WorkoutEntries
            .AsNoTracking()
            .Where(x => x.WorkoutSession.UserId == userId && distinctExerciseIds.Contains(x.ExerciseId))
            .Select(x => new ExerciseMetricRow
            {
                ExerciseId = x.ExerciseId,
                WeightUsedKg = x.WeightUsedKg,
                TimerInSeconds = x.TimerInSeconds,
                HeartRateAvg = x.HeartRateAvg,
                KcalBurned = x.KcalBurned,
                DistanceInMeters = x.DistanceInMeters,
                Speed = x.Speed,
                RateOfPerceivedExertion = x.RateOfPerceivedExertion,
                PerformedAtUtc = x.WorkoutSession.PerformedAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var existingStats = await dbContext.UserExerciseStats
            .Where(x => x.UserId == userId && distinctExerciseIds.Contains(x.ExerciseId))
            .ToDictionaryAsync(x => x.ExerciseId, cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var exerciseId in distinctExerciseIds)
        {
            var exerciseMetrics = metrics
                .Where(x => x.ExerciseId == exerciseId)
                .OrderByDescending(x => x.PerformedAtUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .ToList();

            if (exerciseMetrics.Count == 0)
            {
                if (existingStats.TryGetValue(exerciseId, out var statToRemove))
                {
                    dbContext.UserExerciseStats.Remove(statToRemove);
                }

                continue;
            }

            if (!existingStats.TryGetValue(exerciseId, out var stat))
            {
                stat = new UserExerciseStat
                {
                    UserId = userId,
                    ExerciseId = exerciseId,
                    CreatedAtUtc = now
                };

                dbContext.UserExerciseStats.Add(stat);
            }

            var latestMetric = exerciseMetrics[0];

            stat.UseCount = exerciseMetrics.Count;
            stat.BestWeightKg = exerciseMetrics.Max(x => x.WeightUsedKg);
            stat.AverageWeightKg = exerciseMetrics.Average(x => x.WeightUsedKg);
            stat.LastUsedWeightKg = latestMetric.WeightUsedKg;
            stat.AverageTimerInSeconds = AverageNullable(exerciseMetrics.Select(x => x.TimerInSeconds));
            stat.AverageHeartRate = AverageNullable(exerciseMetrics.Select(x => x.HeartRateAvg));
            stat.AverageKcalBurned = exerciseMetrics.Average(x => (double)x.KcalBurned);
            stat.AverageDistanceMeters = AverageNullable(exerciseMetrics.Select(x => x.DistanceInMeters));
            stat.AverageSpeed = AverageNullable(exerciseMetrics.Select(x => x.Speed));
            stat.AverageRateOfPerceivedExertion = exerciseMetrics.Average(x => x.RateOfPerceivedExertion);
            stat.LastPerformedAtUtc = latestMetric.PerformedAtUtc;
            stat.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static double? AverageNullable(IEnumerable<int?> values)
    {
        var projected = values
            .Where(x => x.HasValue)
            .Select(x => (double)x!.Value)
            .ToList();

        return projected.Count == 0
            ? null
            : projected.Average();
    }

    private static Expression<Func<UserExerciseStat, UserExerciseStatResponse>> MapToResponseExpression()
    {
        return x => new UserExerciseStatResponse
        {
            Id = x.Id,
            UserId = x.UserId,
            ExerciseId = x.ExerciseId,
            ExerciseName = x.Exercise.Name,
            UseCount = x.UseCount,
            BestWeightKg = x.BestWeightKg,
            AverageWeightKg = x.AverageWeightKg,
            LastUsedWeightKg = x.LastUsedWeightKg,
            AverageTimerInSeconds = x.AverageTimerInSeconds,
            AverageHeartRate = x.AverageHeartRate,
            AverageKcalBurned = x.AverageKcalBurned,
            AverageDistanceMeters = x.AverageDistanceMeters,
            AverageSpeed = x.AverageSpeed,
            AverageRateOfPerceivedExertion = x.AverageRateOfPerceivedExertion,
            LastPerformedAtUtc = x.LastPerformedAtUtc,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        };
    }

    private sealed class ExerciseMetricRow
    {
        public int ExerciseId { get; init; }

        public double WeightUsedKg { get; init; }

        public int? TimerInSeconds { get; init; }

        public int? HeartRateAvg { get; init; }

        public int KcalBurned { get; init; }

        public int? DistanceInMeters { get; init; }

        public int? Speed { get; init; }

        public double RateOfPerceivedExertion { get; init; }

        public DateTime PerformedAtUtc { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }
}
