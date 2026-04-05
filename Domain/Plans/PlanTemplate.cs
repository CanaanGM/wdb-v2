namespace Domain.Plans;

public sealed class PlanTemplate
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DurationWeeks { get; set; }

    public string Status { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<PlanDay> Days { get; set; } = new List<PlanDay>();
}
