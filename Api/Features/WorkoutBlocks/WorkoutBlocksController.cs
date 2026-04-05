using Api.Application.Contracts.Querying;
using Api.Features.Auth.Security;
using Api.Features.WorkoutBlocks.Commands.CreateWorkoutBlock;
using Api.Features.WorkoutBlocks.Commands.CreateWorkoutBlocksBulk;
using Api.Features.WorkoutBlocks.Commands.DeleteWorkoutBlock;
using Api.Features.WorkoutBlocks.Commands.UpdateWorkoutBlock;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Queries.GetWorkoutBlockById;
using Api.Features.WorkoutBlocks.Queries.SearchWorkoutBlocks;
using Api.Features.WorkoutBlocks.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.WorkoutBlocks;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WorkoutBlocksController(ISender sender, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType<PagedResponse<WorkoutBlockResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<WorkoutBlockResponse>>> Search(
        [FromBody] SearchWorkoutBlocksRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new SearchWorkoutBlocksQuery(userId.Value, request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<WorkoutBlockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutBlockResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var workoutBlock = await sender.Send(new GetWorkoutBlockByIdQuery(userId.Value, id), cancellationToken);
        if (workoutBlock is null)
        {
            return NotFound();
        }

        return Ok(workoutBlock);
    }

    [HttpPost]
    [ProducesResponseType<WorkoutBlockResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkoutBlockResponse>> Create(
        [FromBody] CreateWorkoutBlockRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new CreateWorkoutBlockCommand(userId.Value, request), cancellationToken);

        if (result.ResultType == WorkoutBlockOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Workout block creation failed.");
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPost("bulk")]
    [ProducesResponseType<CreateWorkoutBlocksBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateWorkoutBlocksBulkResponse>> CreateBulk(
        [FromBody]
        [MinLength(1)]
        List<CreateWorkoutBlockRequest> requests,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new CreateWorkoutBlocksBulkCommand(userId.Value, requests), cancellationToken);

        if (result.ResultType == WorkoutBlockOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        return Ok(new CreateWorkoutBlocksBulkResponse
        {
            CreatedCount = result.Value
        });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<WorkoutBlockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutBlockResponse>> Update(
        int id,
        [FromBody] UpdateWorkoutBlockRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new UpdateWorkoutBlockCommand(userId.Value, id, request), cancellationToken);

        if (result.ResultType == WorkoutBlockOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == WorkoutBlockOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Workout block update failed.");
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new DeleteWorkoutBlockCommand(userId.Value, id), cancellationToken);

        if (result.ResultType == WorkoutBlockOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.ResultType == WorkoutBlockOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
