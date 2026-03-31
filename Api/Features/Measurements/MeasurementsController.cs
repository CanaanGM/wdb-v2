using Api.Features.Auth.Security;
using Api.Features.Measurements.Contracts;
using Api.Features.Measurements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Measurements;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MeasurementsController(
    IMeasurementsService measurementsService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<MeasurementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MeasurementResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await measurementsService.GetAllAsync(userId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeasurementResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await measurementsService.GetByIdAsync(userId.Value, id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeasurementResponse>> Create(
        [FromBody] MeasurementUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await measurementsService.CreateAsync(userId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeasurementResponse>> Update(
        int id,
        [FromBody] MeasurementUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await measurementsService.UpdateAsync(userId.Value, id, request, cancellationToken);
        if (result.ResultType == MeasurementOperationResultType.NotFound)
        {
            return NotFound(result.Error);
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

        var result = await measurementsService.DeleteAsync(userId.Value, id, cancellationToken);
        if (result.ResultType == MeasurementOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }
}
