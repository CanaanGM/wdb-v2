using Domain.Exercises;

namespace Domain.Muscles;

public sealed class Muscle
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string MuscleGroup { get; set; } = string.Empty;

    public string? Function { get; set; }

    public string? WikiPageUrl { get; set; }

    public ICollection<ExerciseMuscle> ExerciseMuscles { get; set; } = new List<ExerciseMuscle>();
}

