using System.ComponentModel.DataAnnotations;

namespace Api.Features.Equipments.Contracts;

public sealed class CreateEquipmentRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? HowTo { get; set; }

    [Range(0d, double.MaxValue)]
    public double? WeightKg { get; set; }
}
