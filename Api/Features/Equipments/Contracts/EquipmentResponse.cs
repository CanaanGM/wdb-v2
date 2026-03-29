namespace Api.Features.Equipments.Contracts;

public sealed class EquipmentResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? HowTo { get; init; }

    public double WeightKg { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
