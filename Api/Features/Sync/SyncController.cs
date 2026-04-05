using Api.Features.Auth.Security;
using Api.Features.Sync.Contracts;
using Api.Features.Sync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Sync;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SyncController(
    ISyncService syncService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType<SyncBootstrapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncBootstrapResponse>> Bootstrap(CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await syncService.BootstrapAsync(userId.Value, cancellationToken);
        return Ok(response);
    }

    [HttpGet("pull")]
    [ProducesResponseType<SyncPullResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncPullResponse>> Pull(
        [FromQuery] long cursor = 0,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (cursor < 0)
        {
            return BadRequest("cursor must be >= 0.");
        }

        if (limit is < 1 or > 2000)
        {
            return BadRequest("limit must be between 1 and 2000.");
        }

        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await syncService.PullAsync(userId.Value, cursor, limit, cancellationToken);
        return Ok(response);
    }

    [HttpPost("push")]
    [ProducesResponseType<SyncPushResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncPushResponse>> Push(
        [FromBody] SyncPushRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Operations.Count == 0)
        {
            return BadRequest("operations must contain at least one item.");
        }

        if (request.Operations.Count > 1000)
        {
            return BadRequest("operations cannot exceed 1000 per request.");
        }

        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await syncService.PushAsync(userId.Value, request, cancellationToken);
        return Ok(response);
    }
}
