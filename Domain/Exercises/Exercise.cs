using Domain.TrainingTypes;

namespace Domain.Exercises;

public sealed class Exercise
{
    public int Id { get; set; }

    public int Difficulty { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? HowTo { get; set; }

    public ICollection<ExerciseHowTo> HowTos { get; set; } = new List<ExerciseHowTo>();

    public ICollection<ExerciseMuscle> ExerciseMuscles { get; set; } = new List<ExerciseMuscle>();

    public ICollection<ExerciseEquipment> ExerciseEquipments { get; set; } = new List<ExerciseEquipment>();

    public ICollection<ExerciseTrainingType> ExerciseTrainingTypes { get; set; } = new List<ExerciseTrainingType>();
}
