using Domain.TrainingTypes;

namespace Domain.Exercises;

public sealed class ExerciseTrainingType
{
    public int ExerciseId { get; set; }

    public int TrainingTypeId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public TrainingType TrainingType { get; set; } = null!;
}
