using System.ComponentModel.DataAnnotations;
using Api.Application.Contracts.Querying;

namespace Api.Features.Plans.Contracts;

public sealed class SearchPlansRequest : PagedFilterRequest
{
    [RegularExpression("^(draft|published|archived)$")]
    public string? Status { get; set; } = "published";
}

public sealed class CreatePlanTemplateRequest : IValidatableObject
{
    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(1, 104)]
    public int DurationWeeks { get; set; }

    [Required]
    [MinLength(1)]
    public List<PlanDayRequest> Days { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicateScheduleKeys = Days
            .GroupBy(x => (x.WeekNumber, x.DayNumber))
            .Where(x => x.Count() > 1)
            .Select(x => $"(week={x.Key.WeekNumber}, day={x.Key.DayNumber})")
            .OrderBy(x => x)
            .ToList();

        if (duplicateScheduleKeys.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate plan day schedule keys: {string.Join(", ", duplicateScheduleKeys)}.",
                [nameof(Days)]);
        }
    }
}

public sealed class UpdatePlanTemplateRequest : IValidatableObject
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(1, 104)]
    public int DurationWeeks { get; set; }

    [Required]
    [MinLength(1)]
    public List<PlanDayRequest> Days { get; set; } = [];

    [Required]
    [RegularExpression("^(draft|published|archived)$")]
    public string Status { get; set; } = "draft";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicateScheduleKeys = Days
            .GroupBy(x => (x.WeekNumber, x.DayNumber))
            .Where(x => x.Count() > 1)
            .Select(x => $"(week={x.Key.WeekNumber}, day={x.Key.DayNumber})")
            .OrderBy(x => x)
            .ToList();

        if (duplicateScheduleKeys.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate plan day schedule keys: {string.Join(", ", duplicateScheduleKeys)}.",
                [nameof(Days)]);
        }
    }
}

public sealed class PlanDayRequest : IValidatableObject
{
    [Range(1, 104)]
    public int WeekNumber { get; set; }

    [Range(1, 7)]
    public int DayNumber { get; set; }

    [MaxLength(255)]
    public string? Title { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<PlanDayExerciseRequest> Exercises { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicateOrderNumbers = Exercises
            .GroupBy(x => x.OrderNumber)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateOrderNumbers.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate exercise order numbers in plan day: {string.Join(", ", duplicateOrderNumbers)}.",
                [nameof(Exercises)]);
        }
    }
}

public sealed class PlanDayExerciseRequest
{
    [Range(1, int.MaxValue)]
    public int ExerciseId { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderNumber { get; set; }

    [Range(0, int.MaxValue)]
    public int? Sets { get; set; }

    [Range(0, int.MaxValue)]
    public int? Repetitions { get; set; }

    [Range(0, 10)]
    public double? TargetRateOfPerceivedExertion { get; set; }

    [Range(0d, double.MaxValue)]
    public double? TargetWeightKg { get; set; }

    [Range(0, int.MaxValue)]
    public int? TimerInSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int? DistanceInMeters { get; set; }

    [Range(0, int.MaxValue)]
    public int? RestInSeconds { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}

public sealed class CreatePlansBulkResponse
{
    public int CreatedCount { get; set; }
}

public sealed class CreatePlanDayExercisesBulkResponse
{
    public int CreatedCount { get; set; }
}

public sealed class PlanTemplateResponse
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DurationWeeks { get; set; }

    public string Status { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PlanTemplateDetailsResponse
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DurationWeeks { get; set; }

    public string Status { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<string> TrainingTypes { get; set; } = [];

    public List<string> RequiredEquipment { get; set; } = [];

    public List<PlanDayResponse> Days { get; set; } = [];
}

public sealed class PlanDayResponse
{
    public int Id { get; set; }

    public int WeekNumber { get; set; }

    public int DayNumber { get; set; }

    public string? Title { get; set; }

    public string? Notes { get; set; }

    public List<PlanDayExerciseResponse> Exercises { get; set; } = [];
}

public sealed class PlanDayExerciseResponse
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public int? Sets { get; set; }

    public int? Repetitions { get; set; }

    public double? TargetRateOfPerceivedExertion { get; set; }

    public double? TargetWeightKg { get; set; }

    public int? TimerInSeconds { get; set; }

    public int? DistanceInMeters { get; set; }

    public int? RestInSeconds { get; set; }

    public string? Notes { get; set; }
}

public sealed class EnrollInPlanRequest
{
    public DateTime? StartedAtUtc { get; set; }

    [MaxLength(100)]
    public string? TimeZoneId { get; set; }
}

public sealed class SearchUserPlanEnrollmentsRequest : PagedFilterRequest
{
    [RegularExpression("^(active|completed|cancelled)$")]
    public string? Status { get; set; } = "active";
}

public sealed class UserPlanEnrollmentResponse
{
    public int Id { get; set; }

    public int PlanTemplateId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public string PlanSlug { get; set; } = string.Empty;

    public int PlanVersion { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public DateOnly StartLocalDate { get; set; }

    public DateOnly EndLocalDateInclusive { get; set; }

    public string Status { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public sealed class GetUserPlanAgendaRequest : IValidatableObject
{
    public DateOnly FromLocalDate { get; set; }

    public DateOnly ToLocalDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromLocalDate > ToLocalDate)
        {
            yield return new ValidationResult(
                "fromLocalDate cannot be later than toLocalDate.",
                [nameof(FromLocalDate), nameof(ToLocalDate)]);
        }

        var days = ToLocalDate.DayNumber - FromLocalDate.DayNumber + 1;
        if (days > 56)
        {
            yield return new ValidationResult(
                "Agenda search range cannot exceed 56 days.",
                [nameof(FromLocalDate), nameof(ToLocalDate)]);
        }
    }
}

public sealed class UserPlanAgendaDayResponse
{
    public int EnrollmentId { get; set; }

    public int PlanTemplateId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public string PlanSlug { get; set; } = string.Empty;

    public int PlanVersion { get; set; }

    public DateOnly LocalDate { get; set; }

    public int WeekNumber { get; set; }

    public int DayNumber { get; set; }

    public string? DayTitle { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsRestDay { get; set; }

    public List<UserPlanAgendaExerciseResponse> Exercises { get; set; } = [];
}

public sealed class UserPlanAgendaExerciseResponse
{
    public int PlanDayExerciseId { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public int? Sets { get; set; }

    public int? Repetitions { get; set; }

    public double? TargetRateOfPerceivedExertion { get; set; }

    public double? TargetWeightKg { get; set; }

    public int? TimerInSeconds { get; set; }

    public int? DistanceInMeters { get; set; }

    public int? RestInSeconds { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = "pending";
}

public sealed class CompletePlanDayRequest
{
    public DateOnly LocalDate { get; set; }

    [Range(1, int.MaxValue)]
    public int? LinkedWorkoutSessionId { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}

public sealed class SkipPlanDayRequest
{
    public DateOnly LocalDate { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}

public sealed class CompletePlanExerciseRequest
{
    public DateOnly LocalDate { get; set; }

    [Range(1, int.MaxValue)]
    public int PlanDayExerciseId { get; set; }

    [Range(1, int.MaxValue)]
    public int? LinkedWorkoutEntryId { get; set; }
}

public sealed class UserPlanDayExecutionResponse
{
    public int EnrollmentId { get; set; }

    public DateOnly LocalDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? LinkedWorkoutSessionId { get; set; }

    public string? Notes { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UserPlanExerciseExecutionResponse
{
    public int EnrollmentId { get; set; }

    public DateOnly LocalDate { get; set; }

    public int PlanDayExerciseId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? LinkedWorkoutEntryId { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
