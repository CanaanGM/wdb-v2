namespace Domain.Exercises;

public sealed class ExerciseHowTo
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public Exercise Exercise { get; set; } = null!;
}
