using Api.Application.Contracts.Querying;
using Api.Features.Auth.Security;
using Api.Features.UserExerciseStats.Contracts;
using Api.Features.UserExerciseStats.Queries.GetUserExerciseStatByExerciseId;
using Api.Features.UserExerciseStats.Queries.SearchUserExerciseStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.UserExerciseStats;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UserExerciseStatsController(ISender sender, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<UserExerciseStatResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<UserExerciseStatResponse>>> SearchByQuery(
        [FromQuery]
        [Range(1, int.MaxValue)]
        int pageNumber = 1,
        [FromQuery]
        [Range(1, 100)]
        int pageSize = 20,
        [FromQuery]
        [StringLength(200)]
        string? search = null,
        [FromQuery]
        [Range(1, int.MaxValue)]
        int? exerciseId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var request = new SearchUserExerciseStatsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            ExerciseId = exerciseId
        };

        var result = await sender.Send(new SearchUserExerciseStatsQuery(userId.Value, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("search")]
    [ProducesResponseType<PagedResponse<UserExerciseStatResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<UserExerciseStatResponse>>> Search(
        [FromBody] SearchUserExerciseStatsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new SearchUserExerciseStatsQuery(userId.Value, request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{exerciseId:int}")]
    [ProducesResponseType<UserExerciseStatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserExerciseStatResponse>> GetByExerciseId(
        int exerciseId,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new GetUserExerciseStatByExerciseIdQuery(userId.Value, exerciseId),
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
