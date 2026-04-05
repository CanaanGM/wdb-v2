using Domain.Exercises;

namespace Domain.Workouts;

public sealed class UserExerciseStat
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ExerciseId { get; set; }

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

    public Exercise Exercise { get; set; } = null!;
}
