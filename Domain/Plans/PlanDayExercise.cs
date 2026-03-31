using Domain.Exercises;

namespace Domain.Plans;

public sealed class PlanDayExercise
{
    public int Id { get; set; }

    public int PlanDayId { get; set; }

    public int ExerciseId { get; set; }

    public int OrderNumber { get; set; }

    public int? Sets { get; set; }

    public int? Repetitions { get; set; }

    public double? TargetRateOfPerceivedExertion { get; set; }

    public double? TargetWeightKg { get; set; }

    public int? TimerInSeconds { get; set; }

    public int? DistanceInMeters { get; set; }

    public int? RestInSeconds { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public PlanDay PlanDay { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
