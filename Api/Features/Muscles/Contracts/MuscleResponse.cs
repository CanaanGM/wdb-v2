namespace Api.Features.Muscles.Contracts;

public sealed class MuscleResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string MuscleGroup { get; init; } = string.Empty;

    public string? Function { get; init; }

    public string? WikiPageUrl { get; init; }
}

