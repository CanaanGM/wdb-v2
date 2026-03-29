using Domain.Muscles;

namespace Domain.Exercises;

public sealed class ExerciseMuscle
{
    public bool IsPrimary { get; set; }

    public int MuscleId { get; set; }

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public Muscle Muscle { get; set; } = null!;
}

