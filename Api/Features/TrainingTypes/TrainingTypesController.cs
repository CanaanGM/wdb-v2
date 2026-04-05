using Api.Features.Auth;
using Api.Features.TrainingTypes.Contracts;
using Api.Features.TrainingTypes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.TrainingTypes;

[ApiController]
[Route("api/[controller]")]
public sealed class TrainingTypesController(ITrainingTypesService trainingTypesService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<TrainingTypeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TrainingTypeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var types = await trainingTypesService.GetAllAsync(cancellationToken);
        return Ok(types);
    }

    [HttpPost]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType<TrainingTypeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrainingTypeResponse>> Create(
        [FromBody] CreateTrainingTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await trainingTypesService.CreateAsync(request, cancellationToken);
        return ToActionResult(
            result,
            success => CreatedAtAction(nameof(GetAll), null, success));
    }

    [HttpPost("bulk")]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType<CreateTrainingTypesBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTrainingTypesBulkResponse>> CreateBulk(
        [FromBody]
        [MinLength(1)]
        List<CreateTrainingTypeRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = await trainingTypesService.CreateBulkAsync(requests, cancellationToken);
        if (result.ResultType == TrainingTypeOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == TrainingTypeOperationResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        return Ok(new CreateTrainingTypesBulkResponse
        {
            CreatedCount = result.Value
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType<TrainingTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrainingTypeResponse>> Update(
        int id,
        [FromBody] UpdateTrainingTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await trainingTypesService.UpdateAsync(id, request, cancellationToken);
        return ToActionResult(result, success => Ok(success));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await trainingTypesService.DeleteAsync(id, cancellationToken);
        if (result.ResultType == TrainingTypeOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    private ActionResult<T> ToActionResult<T>(
        TrainingTypeOperationResult<T> result,
        Func<T, ActionResult<T>> success)
    {
        if (result.ResultType == TrainingTypeOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == TrainingTypeOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.ResultType == TrainingTypeOperationResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Operation failed.");
        }

        return success(result.Value);
    }
}
