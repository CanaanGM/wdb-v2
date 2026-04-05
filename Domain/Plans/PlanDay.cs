namespace Domain.Plans;

public sealed class PlanDay
{
    public int Id { get; set; }

    public int PlanTemplateId { get; set; }

    public int WeekNumber { get; set; }

    public int DayNumber { get; set; }

    public string? Title { get; set; }

    public string? Notes { get; set; }

    public PlanTemplate PlanTemplate { get; set; } = null!;

    public ICollection<PlanDayExercise> Exercises { get; set; } = new List<PlanDayExercise>();
}
