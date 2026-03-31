namespace Domain.Plans;

public sealed class UserPlanEnrollment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PlanTemplateId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public string TimeZoneId { get; set; } = "UTC";

    public DateOnly StartLocalDate { get; set; }

    public DateOnly EndLocalDateInclusive { get; set; }

    public string Status { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public PlanTemplate PlanTemplate { get; set; } = null!;

    public ICollection<UserPlanDayExecution> DayExecutions { get; set; } = new List<UserPlanDayExecution>();
}
