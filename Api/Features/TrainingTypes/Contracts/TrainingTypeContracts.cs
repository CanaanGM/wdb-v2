using System.ComponentModel.DataAnnotations;

namespace Api.Features.TrainingTypes.Contracts;

public sealed class CreateTrainingTypeRequest
{
    [Required]
    [MaxLength(100)]
    [RegularExpression(@".*\S.*")]
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateTrainingTypeRequest
{
    [Required]
    [MaxLength(100)]
    [RegularExpression(@".*\S.*")]
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateTrainingTypesBulkResponse
{
    public int CreatedCount { get; set; }
}

public sealed class TrainingTypeResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
