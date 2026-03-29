using Domain.Exercises;

namespace Domain.Equipments;

public sealed class Equipment
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? HowTo { get; set; }

    public double WeightKg { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<ExerciseEquipment> ExerciseEquipments { get; set; } = new List<ExerciseEquipment>();
}
