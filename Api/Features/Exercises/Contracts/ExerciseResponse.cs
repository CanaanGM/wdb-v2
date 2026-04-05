namespace Api.Features.Exercises.Contracts;

public sealed class ExerciseResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? HowTo { get; init; }

    public int Difficulty { get; init; }

    public List<string> TrainingTypes { get; init; } = [];

    public List<ExerciseHowToResponse> HowTos { get; init; } = [];

    public List<ExerciseMuscleResponse> ExerciseMuscles { get; init; } = [];
}

public sealed class ExerciseHowToResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;
}

public sealed class ExerciseMuscleResponse
{
    public int MuscleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string MuscleGroup { get; init; } = string.Empty;

    public string? Function { get; init; }

    public string? WikiPageUrl { get; init; }

    public bool IsPrimary { get; init; }
}
