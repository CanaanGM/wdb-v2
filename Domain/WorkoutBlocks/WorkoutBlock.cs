namespace Domain.WorkoutBlocks;

public sealed class WorkoutBlock
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Sets { get; set; }

    public int RestInSeconds { get; set; }

    public string? Instructions { get; set; }

    public int OrderNumber { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<WorkoutBlockExercise> BlockExercises { get; set; } = new List<WorkoutBlockExercise>();
}
