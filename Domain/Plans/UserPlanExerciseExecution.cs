using Domain.Workouts;

namespace Domain.Plans;

public sealed class UserPlanExerciseExecution
{
    public int Id { get; set; }

    public int DayExecutionId { get; set; }

    public int PlanDayExerciseId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? LinkedWorkoutEntryId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public UserPlanDayExecution DayExecution { get; set; } = null!;

    public PlanDayExercise PlanDayExercise { get; set; } = null!;

    public WorkoutEntry? LinkedWorkoutEntry { get; set; }
}
