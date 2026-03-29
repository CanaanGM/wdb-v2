using Api.Features.Exercises.Contracts;

namespace Api.Features.Exercises.Services;

public enum CreateExerciseResultType
{
    Success = 0,
    ValidationError = 1,
    Conflict = 2
}

public sealed class CreateExerciseResult
{
    private CreateExerciseResult(CreateExerciseResultType resultType, ExerciseResponse? exercise = null, string? error = null)
    {
        ResultType = resultType;
        Exercise = exercise;
        Error = error;
    }

    public CreateExerciseResultType ResultType { get; }

    public ExerciseResponse? Exercise { get; }

    public string? Error { get; }

    public static CreateExerciseResult Success(ExerciseResponse exercise) =>
        new(CreateExerciseResultType.Success, exercise);

    public static CreateExerciseResult ValidationError(string error) =>
        new(CreateExerciseResultType.ValidationError, error: error);

    public static CreateExerciseResult Conflict(string error) =>
        new(CreateExerciseResultType.Conflict, error: error);
}

