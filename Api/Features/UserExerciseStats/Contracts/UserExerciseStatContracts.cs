using System.ComponentModel.DataAnnotations;
using Api.Application.Contracts.Querying;

namespace Api.Features.UserExerciseStats.Contracts;

public sealed class SearchUserExerciseStatsRequest : PagedFilterRequest
{
    [Range(1, int.MaxValue)]
    public int? ExerciseId { get; set; }
}

public sealed class UserExerciseStatResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int UseCount { get; set; }

    public double BestWeightKg { get; set; }

    public double AverageWeightKg { get; set; }

    public double LastUsedWeightKg { get; set; }

    public double? AverageTimerInSeconds { get; set; }

    public double? AverageHeartRate { get; set; }

    public double? AverageKcalBurned { get; set; }

    public double? AverageDistanceMeters { get; set; }

    public double? AverageSpeed { get; set; }

    public double? AverageRateOfPerceivedExertion { get; set; }

    public DateTime LastPerformedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
