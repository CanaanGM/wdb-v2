using Api.Application.Contracts.Querying;
using Api.Features.Exercises.Commands.CreateExercise;
using Api.Features.Exercises.Commands.CreateExercisesBulk;
using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Queries.GetAllExercises;
using Api.Features.Exercises.Queries.GetExerciseById;
using Api.Features.Exercises.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.Exercises;

[ApiController]
[Route("api/[controller]")]
public sealed class ExercisesController(ISender sender) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType<PagedResponse<ExerciseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ExerciseResponse>>> Search(
        [FromBody] GetExercisesRequest request,
        CancellationToken cancellationToken)
    {
        var exercises = await sender.Send(new GetAllExercisesQuery(request), cancellationToken);

        return Ok(exercises);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var exercise = await sender.Send(new GetExerciseByIdQuery(id), cancellationToken);

        if (exercise is null)
        {
            return NotFound();
        }

        return Ok(exercise);
    }

    [HttpPost]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExerciseResponse>> Create(
        [FromBody] CreateExerciseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateExerciseCommand(request), cancellationToken);

        if (result.ResultType == CreateExerciseResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == CreateExerciseResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        if (result.Exercise is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Exercise creation failed.");
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Exercise.Id }, result.Exercise);
    }

    [HttpPost("bulk")]
    [ProducesResponseType<CreateExercisesBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateExercisesBulkResponse>> CreateBulk(
        [FromBody]
        [MinLength(1)]
        List<CreateExerciseRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateExercisesBulkCommand(requests), cancellationToken);

        if (result.ResultType == CreateExercisesBulkResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == CreateExercisesBulkResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        return Ok(new CreateExercisesBulkResponse
        {
            CreatedCount = result.CreatedCount
        });
    }
}
