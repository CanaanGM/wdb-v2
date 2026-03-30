using Domain.Exercises;

namespace Domain.Workouts;

public sealed class WorkoutEntry
{
    public int Id { get; set; }

    public int WorkoutSessionId { get; set; }

    public int ExerciseId { get; set; }

    public int OrderNumber { get; set; }

    public int Repetitions { get; set; }

    public int Mood { get; set; }

    public int? TimerInSeconds { get; set; }

    public double WeightUsedKg { get; set; }

    public double RateOfPerceivedExertion { get; set; }

    public int? RestInSeconds { get; set; }

    public int KcalBurned { get; set; }

    public int? DistanceInMeters { get; set; }

    public string? Notes { get; set; }

    public int? Incline { get; set; }

    public int? Speed { get; set; }

    public int? HeartRateAvg { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public WorkoutSession WorkoutSession { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
