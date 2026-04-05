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
            .GroupBy(x => new
            {
                UserId = x.WorkoutSession.UserId,
                x.ExerciseId
            })
            .Select(x => new AggregatedMetricRow
            {
                UserId = x.Key.UserId,
                ExerciseId = x.Key.ExerciseId,
                UseCount = x.Count(),
                BestWeightKg = x.Max(y => y.WeightUsedKg),
                AverageWeightKg = x.Average(y => y.WeightUsedKg),
                LastUsedWeightKg = x
                    .OrderByDescending(y => y.WorkoutSession.PerformedAtUtc)
                    .ThenByDescending(y => y.OrderNumber)
                    .ThenByDescending(y => y.CreatedAtUtc)
                    .ThenByDescending(y => y.Id)
                    .Select(y => y.WeightUsedKg)
                    .FirstOrDefault(),
                AverageTimerInSeconds = x.Average(y => (double?)y.TimerInSeconds),
                AverageHeartRate = x.Average(y => (double?)y.HeartRateAvg),
                AverageKcalBurned = x.Average(y => (double?)y.KcalBurned),
                AverageDistanceMeters = x.Average(y => (double?)y.DistanceInMeters),
                AverageSpeed = x.Average(y => (double?)y.Speed),
                AverageRateOfPerceivedExertion = x.Average(y => (double?)y.RateOfPerceivedExertion),
                LastPerformedAtUtc = x.Max(y => y.WorkoutSession.PerformedAtUtc)
            })
            .ToListAsync(cancellationToken);

        var metricsByKey = metrics
            .ToDictionary(x => new UserExerciseKey(x.UserId, x.ExerciseId));

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
            if (!metricsByKey.TryGetValue(key, out var exerciseMetrics))
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

            stat.UseCount = exerciseMetrics.UseCount;
            stat.BestWeightKg = exerciseMetrics.BestWeightKg;
            stat.AverageWeightKg = exerciseMetrics.AverageWeightKg;
            stat.LastUsedWeightKg = exerciseMetrics.LastUsedWeightKg;
            stat.AverageTimerInSeconds = exerciseMetrics.AverageTimerInSeconds;
            stat.AverageHeartRate = exerciseMetrics.AverageHeartRate;
            stat.AverageKcalBurned = exerciseMetrics.AverageKcalBurned;
            stat.AverageDistanceMeters = exerciseMetrics.AverageDistanceMeters;
            stat.AverageSpeed = exerciseMetrics.AverageSpeed;
            stat.AverageRateOfPerceivedExertion = exerciseMetrics.AverageRateOfPerceivedExertion;
            stat.LastPerformedAtUtc = exerciseMetrics.LastPerformedAtUtc;
            stat.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private sealed class AggregatedMetricRow
    {
        public int UserId { get; init; }

        public int ExerciseId { get; init; }

        public int UseCount { get; init; }

        public double BestWeightKg { get; init; }

        public double AverageWeightKg { get; init; }

        public double LastUsedWeightKg { get; init; }

        public double? AverageTimerInSeconds { get; init; }

        public double? AverageHeartRate { get; init; }

        public double? AverageKcalBurned { get; init; }

        public double? AverageDistanceMeters { get; init; }

        public double? AverageSpeed { get; init; }

        public double? AverageRateOfPerceivedExertion { get; init; }

        public DateTime LastPerformedAtUtc { get; init; }
    }

    private readonly record struct UserExerciseKey(int UserId, int ExerciseId);
}
