using Api.Application.Cqrs;
using Api.Features.Muscles.Contracts;
using Api.Features.Muscles.Services;

namespace Api.Features.Muscles.Commands.CreateMusclesBulk;

public sealed record CreateMusclesBulkCommand(List<CreateMuscleRequest> Requests)
    : ICommand<CreateMusclesBulkResult>;

