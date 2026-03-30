using System.ComponentModel.DataAnnotations;
using Api.Application.Contracts.Querying;

namespace Api.Features.WorkoutBlocks.Contracts;

public sealed class SearchWorkoutBlocksRequest : PagedFilterRequest;

public sealed class CreateWorkoutBlockRequest : IValidatableObject
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Sets { get; set; }

    [Range(0, int.MaxValue)]
    public int RestInSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderNumber { get; set; }

    [MaxLength(4000)]
    public string? Instructions { get; set; }

    [Required]
    [MinLength(1)]
    public List<WorkoutBlockExerciseRequest> BlockExercises { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in ValidateDuplicateOrderNumbers(BlockExercises))
        {
            yield return validationResult;
        }
    }

    internal static IEnumerable<ValidationResult> ValidateDuplicateOrderNumbers(IReadOnlyCollection<WorkoutBlockExerciseRequest> blockExercises)
    {
        var duplicateOrderNumbers = blockExercises
            .GroupBy(x => x.OrderNumber)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateOrderNumbers.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate block exercise order numbers: {string.Join(", ", duplicateOrderNumbers)}.",
                [nameof(BlockExercises)]);
        }
    }
}

public sealed class UpdateWorkoutBlockRequest : IValidatableObject
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Sets { get; set; }

    [Range(0, int.MaxValue)]
    public int RestInSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderNumber { get; set; }

    [MaxLength(4000)]
    public string? Instructions { get; set; }

    [Required]
    [MinLength(1)]
    public List<WorkoutBlockExerciseRequest> BlockExercises { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in CreateWorkoutBlockRequest.ValidateDuplicateOrderNumbers(BlockExercises))
        {
            yield return validationResult;
        }
    }
}

public sealed class WorkoutBlockExerciseRequest
{
    [Range(1, int.MaxValue)]
    public int ExerciseId { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderNumber { get; set; }

    [MaxLength(4000)]
    public string? Instructions { get; set; }

    [Range(0, int.MaxValue)]
    public int? Repetitions { get; set; }

    [Range(0, int.MaxValue)]
    public int? TimerInSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int? DistanceInMeters { get; set; }
}

public sealed class WorkoutBlockResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Sets { get; set; }

    public int RestInSeconds { get; set; }

    public int OrderNumber { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<WorkoutBlockExerciseResponse> BlockExercises { get; set; } = [];
}

public sealed class WorkoutBlockExerciseResponse
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public string? Instructions { get; set; }

    public int? Repetitions { get; set; }

    public int? TimerInSeconds { get; set; }

    public int? DistanceInMeters { get; set; }
}

public sealed class CreateWorkoutBlocksBulkResponse
{
    public int CreatedCount { get; set; }
}
