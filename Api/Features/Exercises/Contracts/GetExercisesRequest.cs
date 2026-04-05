using Api.Application.Contracts.Querying;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.Exercises.Contracts;

public sealed class GetExercisesRequest : PagedFilterRequest
{
    [Range(0, 5)]
    public int? Difficulty { get; set; }

    [MaxLength(255)]
    public string? MuscleName { get; set; }

    [MaxLength(100)]
    public string? MuscleGroup { get; set; }

    [MaxLength(100)]
    public string? TrainingTypeName { get; set; }

    public bool? IsPrimary { get; set; }
}
