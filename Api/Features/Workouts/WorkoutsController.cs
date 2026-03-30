using Api.Application.Contracts.Querying;
using Api.Features.Auth.Security;
using Api.Features.Workouts.Commands.CreateWorkout;
using Api.Features.Workouts.Commands.CreateWorkoutsBulk;
using Api.Features.Workouts.Commands.DeleteWorkout;
using Api.Features.Workouts.Commands.UpdateWorkout;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Queries.GetRecentWorkouts;
using Api.Features.Workouts.Queries.GetWorkoutById;
using Api.Features.Workouts.Queries.SearchWorkouts;
using Api.Features.Workouts.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.Workouts;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WorkoutsController(ISender sender, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType<PagedResponse<WorkoutResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<WorkoutResponse>>> Search(
        [FromBody] SearchWorkoutsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var workouts = await sender.Send(new SearchWorkoutsQuery(userId.Value, request), cancellationToken);
        return Ok(workouts);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<WorkoutResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var workout = await sender.Send(new GetWorkoutByIdQuery(userId.Value, id), cancellationToken);
        if (workout is null)
        {
            return NotFound();
        }

        return Ok(workout);
    }

    [HttpGet("recent")]
    [ProducesResponseType<List<WorkoutResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WorkoutResponse>>> GetRecent(
        [FromQuery]
        [Range(1, 24 * 30)]
        int hours = 24,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var workouts = await sender.Send(new GetRecentWorkoutsQuery(userId.Value, hours), cancellationToken);
        return Ok(workouts);
    }

    [HttpPost]
    [ProducesResponseType<WorkoutResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkoutResponse>> Create(
        [FromBody] CreateWorkoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new CreateWorkoutCommand(userId.Value, request), cancellationToken);

        if (result.ResultType == WorkoutOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Workout creation failed.");
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPost("bulk")]
    [ProducesResponseType<CreateWorkoutsBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateWorkoutsBulkResponse>> CreateBulk(
        [FromBody]
        [MinLength(1)]
        List<CreateWorkoutRequest> requests,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new CreateWorkoutsBulkCommand(userId.Value, requests), cancellationToken);

        if (result.ResultType == WorkoutOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        return Ok(new CreateWorkoutsBulkResponse
        {
            CreatedCount = result.Value
        });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<WorkoutResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutResponse>> Update(
        int id,
        [FromBody] UpdateWorkoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new UpdateWorkoutCommand(userId.Value, id, request), cancellationToken);

        if (result.ResultType == WorkoutOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == WorkoutOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Workout update failed.");
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

        var result = await sender.Send(new DeleteWorkoutCommand(userId.Value, id), cancellationToken);

        if (result.ResultType == WorkoutOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.ResultType == WorkoutOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
