using Api.Application.Cqrs;
using Api.Features.Exercises.Services;

namespace Api.Features.Exercises.Commands.CreateExercise;

public sealed class CreateExerciseCommandHandler(IExercisesService exercisesService)
    : ICommandHandler<CreateExerciseCommand, CreateExerciseResult>
{
    public async Task<CreateExerciseResult> Handle(CreateExerciseCommand command, CancellationToken cancellationToken)
    {
        return await exercisesService.CreateAsync(command.Request, cancellationToken);
    }
}

