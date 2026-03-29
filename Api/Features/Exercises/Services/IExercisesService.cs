using Api.Application.Contracts.Querying;
using Api.Features.Exercises.Contracts;

namespace Api.Features.Exercises.Services;

public interface IExercisesService
{
    Task<PagedResponse<ExerciseResponse>> GetAllAsync(GetExercisesRequest request, CancellationToken cancellationToken);

    Task<ExerciseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CreateExerciseResult> CreateAsync(CreateExerciseRequest request, CancellationToken cancellationToken);

    Task<CreateExercisesBulkResult> CreateBulkAsync(
        List<CreateExerciseRequest> requests,
        CancellationToken cancellationToken);
}
