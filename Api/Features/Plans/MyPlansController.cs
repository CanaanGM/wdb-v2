using Api.Application.Contracts.Querying;
using Api.Features.Auth.Security;
using Api.Features.Plans.Contracts;
using Api.Features.Plans.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Plans;

[ApiController]
[Route("api/myplans")]
[Authorize]
public sealed class MyPlansController(
    IPlansService plansService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost("enrollments/search")]
    [ProducesResponseType<PagedResponse<UserPlanEnrollmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<UserPlanEnrollmentResponse>>> SearchEnrollments(
        [FromBody] SearchUserPlanEnrollmentsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await plansService.SearchEnrollmentsAsync(userId.Value, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("agenda/search")]
    [ProducesResponseType<List<UserPlanAgendaDayResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<UserPlanAgendaDayResponse>>> SearchAgenda(
        [FromBody] GetUserPlanAgendaRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await plansService.GetAgendaAsync(userId.Value, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("enrollments/{enrollmentId:int}/days/complete")]
    [ProducesResponseType<UserPlanDayExecutionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlanDayExecutionResponse>> CompleteDay(
        int enrollmentId,
        [FromBody] CompletePlanDayRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await plansService.CompleteDayAsync(userId.Value, enrollmentId, request, cancellationToken);
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
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to complete plan day.");
        }

        return Ok(result.Value);
    }

    [HttpPost("enrollments/{enrollmentId:int}/days/skip")]
    [ProducesResponseType<UserPlanDayExecutionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlanDayExecutionResponse>> SkipDay(
        int enrollmentId,
        [FromBody] SkipPlanDayRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await plansService.SkipDayAsync(userId.Value, enrollmentId, request, cancellationToken);
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
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to skip plan day.");
        }

        return Ok(result.Value);
    }

    [HttpPost("enrollments/{enrollmentId:int}/exercises/complete")]
    [ProducesResponseType<UserPlanExerciseExecutionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlanExerciseExecutionResponse>> CompleteExercise(
        int enrollmentId,
        [FromBody] CompletePlanExerciseRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await plansService.CompleteExerciseAsync(userId.Value, enrollmentId, request, cancellationToken);
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
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to complete plan exercise.");
        }

        return Ok(result.Value);
    }

    [HttpPost("enrollments/{enrollmentId:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrollment(
        int enrollmentId,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await plansService.CancelEnrollmentAsync(userId.Value, enrollmentId, cancellationToken);
        if (result.ResultType == PlanOperationResultType.NotFound)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }
}
