namespace Infrastructure.Persistence.Entities;

public sealed class MigrationProbe
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
