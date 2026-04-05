using System.ComponentModel.DataAnnotations;
using Api.Application.Contracts.Querying;

namespace Api.Features.Workouts.Contracts;

public sealed class SearchWorkoutsRequest : PagedFilterRequest, IValidatableObject
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    [Range(1, int.MaxValue)]
    public int? ExerciseId { get; set; }

    [Range(0, 10)]
    public int? MinMood { get; set; }

    [Range(0, 10)]
    public int? MaxMood { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromUtc.HasValue && ToUtc.HasValue && FromUtc > ToUtc)
        {
            yield return new ValidationResult("fromUtc cannot be later than toUtc.", [nameof(FromUtc), nameof(ToUtc)]);
        }

        if (MinMood.HasValue && MaxMood.HasValue && MinMood > MaxMood)
        {
            yield return new ValidationResult("minMood cannot be greater than maxMood.", [nameof(MinMood), nameof(MaxMood)]);
        }
    }
}

public sealed class CreateWorkoutRequest : IValidatableObject
{
    [Required]
    [MaxLength(100)]
    public string Feeling { get; set; } = "good";

    [Range(0, int.MaxValue)]
    public int DurationInMinutes { get; set; }

    [Range(0, 10)]
    public int Mood { get; set; } = 5;

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public DateTime? PerformedAtUtc { get; set; }

    [Required]
    [MinLength(1)]
    public List<WorkoutEntryRequest> Entries { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in ValidateDuplicateOrderNumbers(Entries))
        {
            yield return validationResult;
        }
    }

    internal static IEnumerable<ValidationResult> ValidateDuplicateOrderNumbers(IReadOnlyCollection<WorkoutEntryRequest> entries)
    {
        var duplicateOrderNumbers = entries
            .GroupBy(x => x.OrderNumber)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateOrderNumbers.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate entry order numbers: {string.Join(", ", duplicateOrderNumbers)}.",
                [nameof(Entries)]);
        }
    }
}

public sealed class UpdateWorkoutRequest : IValidatableObject
{
    [Required]
    [MaxLength(100)]
    public string Feeling { get; set; } = "good";

    [Range(0, int.MaxValue)]
    public int DurationInMinutes { get; set; }

    [Range(0, 10)]
    public int Mood { get; set; } = 5;

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public DateTime? PerformedAtUtc { get; set; }

    [Required]
    [MinLength(1)]
    public List<WorkoutEntryRequest> Entries { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in CreateWorkoutRequest.ValidateDuplicateOrderNumbers(Entries))
        {
            yield return validationResult;
        }
    }
}

public sealed class WorkoutEntryRequest
{
    [Range(1, int.MaxValue)]
    public int ExerciseId { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderNumber { get; set; }

    [Range(0, int.MaxValue)]
    public int Repetitions { get; set; }

    [Range(0, 10)]
    public int Mood { get; set; } = 5;

    [Range(0, int.MaxValue)]
    public int? TimerInSeconds { get; set; }

    [Range(0d, double.MaxValue)]
    public double WeightUsedKg { get; set; }

    [Range(0d, 10d)]
    public double RateOfPerceivedExertion { get; set; }

    [Range(0, int.MaxValue)]
    public int? RestInSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int KcalBurned { get; set; }

    [Range(0, int.MaxValue)]
    public int? DistanceInMeters { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    [Range(0, int.MaxValue)]
    public int? Incline { get; set; }

    [Range(0, int.MaxValue)]
    public int? Speed { get; set; }

    [Range(0, int.MaxValue)]
    public int? HeartRateAvg { get; set; }
}

public sealed class WorkoutResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Feeling { get; set; } = string.Empty;

    public int Mood { get; set; }

    public string? Notes { get; set; }

    public double DurationInMinutes { get; set; }

    public int TotalCaloriesBurned { get; set; }

    public double TotalKgMoved { get; set; }

    public int TotalRepetitions { get; set; }

    public double AverageRateOfPerceivedExertion { get; set; }

    public DateTime PerformedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<WorkoutEntryResponse> Entries { get; set; } = [];
}

public sealed class WorkoutEntryResponse
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public int Repetitions { get; set; }

    public int Mood { get; set; }

    public int? TimerInSeconds { get; set; }

    public double WeightUsedKg { get; set; }

    public double RateOfPerceivedExertion { get; set; }

    public int? RestInSeconds { get; set; }

    public int KcalBurned { get; set; }

    public int? DistanceInMeters { get; set; }

    public string? Notes { get; set; }

    public int? Incline { get; set; }

    public int? Speed { get; set; }

    public int? HeartRateAvg { get; set; }
}

public sealed class CreateWorkoutsBulkResponse
{
    public int CreatedCount { get; set; }
}
