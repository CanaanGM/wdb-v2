using Api.Application.Contracts.Querying;
using Api.Features.Auth;
using Api.Features.Auth.Security;
using Api.Features.Plans.Contracts;
using Api.Features.Plans.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.Plans;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PlansController(
    IPlansService plansService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType<PagedResponse<PlanTemplateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<PlanTemplateResponse>>> Search(
        [FromBody] SearchPlansRequest request,
        CancellationToken cancellationToken)
    {
        var result = await plansService.SearchPlansAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<PlanTemplateDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PlanTemplateDetailsResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await plansService.GetPlanByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType<PlanTemplateDetailsResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlanTemplateDetailsResponse>> Create(
        [FromBody] CreatePlanTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await plansService.CreatePlanAsync(request, cancellationToken);
        if (result.ResultType == PlanOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Plan creation failed.");
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType<CreatePlansBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreatePlansBulkResponse>> CreateBulk(
        [FromBody]
        [MinLength(1)]
        List<CreatePlanTemplateRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = await plansService.CreatePlansBulkAsync(requests, cancellationToken);
        if (result.ResultType == PlanOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        return Ok(new CreatePlansBulkResponse
        {
            CreatedCount = result.Value
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType<PlanTemplateDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanTemplateDetailsResponse>> Update(
        int id,
        [FromBody] UpdatePlanTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await plansService.UpdatePlanAsync(id, request, cancellationToken);
        if (result.ResultType == PlanOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == PlanOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Plan update failed.");
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:int}/weeks/{weekNumber:int}/days/{dayNumber:int}/exercises/bulk")]
    [Authorize(Roles = AuthRoles.Admin)]
    [ProducesResponseType<CreatePlanDayExercisesBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatePlanDayExercisesBulkResponse>> CreateDayExercisesBulk(
        int id,
        [Range(1, 104)] int weekNumber,
        [Range(1, 7)] int dayNumber,
        [FromBody]
        [MinLength(1)]
        List<PlanDayExerciseRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = await plansService.CreatePlanDayExercisesBulkAsync(
            id,
            weekNumber,
            dayNumber,
            requests,
            cancellationToken);
        if (result.ResultType == PlanOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.ResultType == PlanOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        return Ok(new CreatePlanDayExercisesBulkResponse
        {
            CreatedCount = result.Value
        });
    }

    [HttpPost("{id:int}/enroll")]
    [ProducesResponseType<UserPlanEnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlanEnrollmentResponse>> Enroll(
        int id,
        [FromBody] EnrollInPlanRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await plansService.EnrollAsync(userId.Value, id, request, cancellationToken);
        if (result.ResultType == PlanOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.ResultType == PlanOperationResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Plan enrollment failed.");
        }

        return Ok(result.Value);
    }
}
