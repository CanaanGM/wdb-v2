using Domain.Exercises;

namespace Domain.Workouts;

public sealed class WorkoutSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Mood { get; set; }

    public string Feeling { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int DurationInSeconds { get; set; }

    public int Calories { get; set; }

    public double TotalKgMoved { get; set; }

    public int TotalRepetitions { get; set; }

    public double AverageRateOfPerceivedExertion { get; set; }

    public DateTime PerformedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<WorkoutEntry> Entries { get; set; } = new List<WorkoutEntry>();
}
