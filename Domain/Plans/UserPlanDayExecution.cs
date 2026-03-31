using Domain.Workouts;

namespace Domain.Plans;

public sealed class UserPlanDayExecution
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    public DateOnly LocalDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? LinkedWorkoutSessionId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public UserPlanEnrollment Enrollment { get; set; } = null!;

    public WorkoutSession? LinkedWorkoutSession { get; set; }

    public ICollection<UserPlanExerciseExecution> ExerciseExecutions { get; set; } = new List<UserPlanExerciseExecution>();
}
