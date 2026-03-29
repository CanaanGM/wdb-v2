using Api.Application.Cqrs;
using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Services;

namespace Api.Features.Exercises.Queries.GetExerciseById;

public sealed class GetExerciseByIdQueryHandler(IExercisesService exercisesService)
    : IQueryHandler<GetExerciseByIdQuery, ExerciseResponse?>
{
    public async Task<ExerciseResponse?> Handle(GetExerciseByIdQuery query, CancellationToken cancellationToken)
    {
        return await exercisesService.GetByIdAsync(query.Id, cancellationToken);
    }
}

