using System.ComponentModel.DataAnnotations;

namespace Api.Features.Exercises.Contracts;

public sealed class CreateExerciseRequest : IValidatableObject
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? HowTo { get; set; }

    [Range(0, 5)]
    public int Difficulty { get; set; }

    [Required]
    public List<CreateExerciseHowToRequest> HowTos { get; set; } = [];

    [Required]
    [MinLength(1)]
    public List<CreateExerciseMuscleRequest> ExerciseMuscles { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicateMuscleNames = ExerciseMuscles
            .Where(x => !string.IsNullOrWhiteSpace(x.MuscleName))
            .Select(x => x.MuscleName.Trim())
            .GroupBy(x => x, StringComparer.InvariantCultureIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateMuscleNames.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate muscles in exercise: {string.Join(", ", duplicateMuscleNames)}.",
                [nameof(ExerciseMuscles)]);
        }
    }
}

public sealed class CreateExerciseHowToRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    [Url]
    public string Url { get; set; } = string.Empty;
}

public sealed class CreateExerciseMuscleRequest
{
    [Required]
    [MaxLength(255)]
    public string MuscleName { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}
