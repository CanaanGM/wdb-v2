using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Services;

namespace Api.Features.Exercises.Queries.GetAllExercises;

public sealed class GetAllExercisesQueryHandler(IExercisesService exercisesService)
    : IQueryHandler<GetAllExercisesQuery, PagedResponse<ExerciseResponse>>
{
    public async Task<PagedResponse<ExerciseResponse>> Handle(GetAllExercisesQuery query, CancellationToken cancellationToken)
    {
        return await exercisesService.GetAllAsync(query.Request, cancellationToken);
    }
}
