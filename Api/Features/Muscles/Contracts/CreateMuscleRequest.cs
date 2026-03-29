using System.ComponentModel.DataAnnotations;

namespace Api.Features.Muscles.Contracts;

public sealed class CreateMuscleRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string MuscleGroup { get; set; } = string.Empty;

    public string? Function { get; set; }

    [MaxLength(2000)]
    [Url]
    public string? WikiPageUrl { get; set; }
}
