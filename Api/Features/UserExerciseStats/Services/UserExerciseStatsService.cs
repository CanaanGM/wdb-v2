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

        await RecomputeAsync(userId, distinctExerciseIds, cancellationToken);
    }

    public async Task RecomputeAllAsync(CancellationToken cancellationToken)
    {
        await RecomputeAsync(userId: null, exerciseIds: null, cancellationToken);
    }

    private async Task RecomputeAsync(
        int? userId,
        IReadOnlyCollection<int>? exerciseIds,
        CancellationToken cancellationToken)
    {
        var metricsQuery = dbContext.WorkoutEntries
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue)
        {
            var value = userId.Value;
            metricsQuery = metricsQuery.Where(x => x.WorkoutSession.UserId == value);
        }

        if (exerciseIds is { Count: > 0 })
        {
            metricsQuery = metricsQuery.Where(x => exerciseIds.Contains(x.ExerciseId));
        }

        var metrics = await metricsQuery
            .Select(x => new ExerciseMetricRow
            {
                EntryId = x.Id,
                UserId = x.WorkoutSession.UserId,
                ExerciseId = x.ExerciseId,
                OrderNumber = x.OrderNumber,
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

        var metricsByKey = metrics
            .GroupBy(x => new UserExerciseKey(x.UserId, x.ExerciseId))
            .ToDictionary(x => x.Key, x => x.ToList());

        var existingStatsQuery = dbContext.UserExerciseStats.AsQueryable();
        if (userId.HasValue)
        {
            var value = userId.Value;
            existingStatsQuery = existingStatsQuery.Where(x => x.UserId == value);
        }

        if (exerciseIds is { Count: > 0 })
        {
            existingStatsQuery = existingStatsQuery.Where(x => exerciseIds.Contains(x.ExerciseId));
        }

        var existingStats = await existingStatsQuery
            .ToDictionaryAsync(x => new UserExerciseKey(x.UserId, x.ExerciseId), cancellationToken);

        var now = DateTime.UtcNow;
        var allKeys = metricsByKey.Keys
            .Union(existingStats.Keys)
            .ToList();

        foreach (var key in allKeys)
        {
            if (!metricsByKey.TryGetValue(key, out var exerciseMetrics) || exerciseMetrics.Count == 0)
            {
                if (existingStats.TryGetValue(key, out var statToRemove))
                {
                    dbContext.UserExerciseStats.Remove(statToRemove);
                }

                continue;
            }

            if (!existingStats.TryGetValue(key, out var stat))
            {
                stat = new UserExerciseStat
                {
                    UserId = key.UserId,
                    ExerciseId = key.ExerciseId,
                    CreatedAtUtc = now
                };

                dbContext.UserExerciseStats.Add(stat);
            }

            var orderedMetrics = exerciseMetrics
                .OrderByDescending(x => x.PerformedAtUtc)
                .ThenByDescending(x => x.OrderNumber)
                .ThenByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.EntryId)
                .ToList();

            var latestMetric = orderedMetrics[0];

            stat.UseCount = orderedMetrics.Count;
            stat.BestWeightKg = orderedMetrics.Max(x => x.WeightUsedKg);
            stat.AverageWeightKg = orderedMetrics.Average(x => x.WeightUsedKg);
            stat.LastUsedWeightKg = latestMetric.WeightUsedKg;
            stat.AverageTimerInSeconds = AverageNullable(orderedMetrics.Select(x => x.TimerInSeconds));
            stat.AverageHeartRate = AverageNullable(orderedMetrics.Select(x => x.HeartRateAvg));
            stat.AverageKcalBurned = orderedMetrics.Average(x => (double)x.KcalBurned);
            stat.AverageDistanceMeters = AverageNullable(orderedMetrics.Select(x => x.DistanceInMeters));
            stat.AverageSpeed = AverageNullable(orderedMetrics.Select(x => x.Speed));
            stat.AverageRateOfPerceivedExertion = orderedMetrics.Average(x => x.RateOfPerceivedExertion);
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
        public int EntryId { get; init; }

        public int UserId { get; init; }

        public int ExerciseId { get; init; }

        public int OrderNumber { get; init; }

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

    private readonly record struct UserExerciseKey(int UserId, int ExerciseId);
}
