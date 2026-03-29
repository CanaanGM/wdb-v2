using Api.Features.Muscles.Commands.CreateMusclesBulk;
using Api.Features.Muscles.Contracts;
using Api.Features.Muscles.Queries.GetAllMuscles;
using Api.Features.Muscles.Queries.GetMusclesByGroup;
using Api.Features.Muscles.Queries.SearchMuscles;
using Api.Features.Muscles.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.Muscles;

[ApiController]
[Route("api/[controller]")]
public sealed class MusclesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<MuscleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MuscleResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var muscles = await sender.Send(new GetAllMusclesQuery(), cancellationToken);
        return Ok(muscles);
    }

    [HttpGet("search/{searchTerm}")]
    [ProducesResponseType<List<MuscleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MuscleResponse>>> Search(string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term is required.");
        }

        var muscles = await sender.Send(new SearchMusclesQuery(searchTerm), cancellationToken);
        return Ok(muscles);
    }

    [HttpGet("{groupName}")]
    [ProducesResponseType<List<MuscleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MuscleResponse>>> GetByGroup(string groupName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return BadRequest("Group name is required.");
        }

        var muscles = await sender.Send(new GetMusclesByGroupQuery(groupName), cancellationToken);
        return Ok(muscles);
    }

    [HttpPost("bulk")]
    [ProducesResponseType<CreateMusclesBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateMusclesBulkResponse>> CreateBulk(
        [FromBody]
        [MinLength(1)]
        List<CreateMuscleRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateMusclesBulkCommand(requests), cancellationToken);

        if (result.ResultType == CreateMusclesBulkResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == CreateMusclesBulkResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        return Ok(new CreateMusclesBulkResponse
        {
            CreatedCount = result.CreatedCount
        });
    }
}
