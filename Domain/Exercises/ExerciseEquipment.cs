using Domain.Equipments;

namespace Domain.Exercises;

public sealed class ExerciseEquipment
{
    public int ExerciseId { get; set; }

    public int EquipmentId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public Equipment Equipment { get; set; } = null!;
}
