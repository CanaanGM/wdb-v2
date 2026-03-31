using Domain.Exercises;

namespace Domain.TrainingTypes;

public sealed class TrainingType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<ExerciseTrainingType> ExerciseTrainingTypes { get; set; } = new List<ExerciseTrainingType>();
}
