using Api.Application.Cqrs;
using Api.Features.Muscles.Services;

namespace Api.Features.Muscles.Commands.CreateMusclesBulk;

public sealed class CreateMusclesBulkCommandHandler(IMusclesService musclesService)
    : ICommandHandler<CreateMusclesBulkCommand, CreateMusclesBulkResult>
{
    public async Task<CreateMusclesBulkResult> Handle(
        CreateMusclesBulkCommand command,
        CancellationToken cancellationToken)
    {
        return await musclesService.CreateBulkAsync(command.Requests, cancellationToken);
    }
}

