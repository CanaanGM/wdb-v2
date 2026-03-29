using Api.Application.Cqrs;
using Api.Features.Exercises.Services;

namespace Api.Features.Exercises.Commands.CreateExercisesBulk;

public sealed class CreateExercisesBulkCommandHandler(IExercisesService exercisesService)
    : ICommandHandler<CreateExercisesBulkCommand, CreateExercisesBulkResult>
{
    public async Task<CreateExercisesBulkResult> Handle(
        CreateExercisesBulkCommand command,
        CancellationToken cancellationToken)
    {
        return await exercisesService.CreateBulkAsync(command.Requests, cancellationToken);
    }
}

