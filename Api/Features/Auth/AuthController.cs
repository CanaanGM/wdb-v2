using Api.Features.Auth.Commands.ForgotPassword;
using Api.Features.Auth.Commands.Login;
using Api.Features.Auth.Commands.Logout;
using Api.Features.Auth.Commands.Refresh;
using Api.Features.Auth.Commands.Register;
using Api.Features.Auth.Commands.ResetPassword;
using Api.Features.Auth.Contracts;
using Api.Features.Auth.Options;
using Api.Features.Auth.Queries.GetCurrentUser;
using Api.Features.Auth.Security;
using Api.Features.Auth.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ISender sender,
    ICurrentUserAccessor currentUserAccessor,
    IHostEnvironment hostEnvironment,
    IOptions<PasswordResetOptions> passwordResetOptions) : ControllerBase
{
    private readonly PasswordResetOptions _passwordResetOptions = passwordResetOptions.Value;

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterCommand(request), cancellationToken);
        return ToAuthActionResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request), cancellationToken);
        return ToAuthActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is missing.");
        }

        var result = await sender.Send(new RefreshCommand(request.RefreshToken), cancellationToken);
        return ToAuthActionResult(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is missing.");
        }

        await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await sender.Send(new GetCurrentUserQuery(userId.Value), cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType<ForgotPasswordResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var includeDebugResetToken = hostEnvironment.IsDevelopment()
            && _passwordResetOptions.IncludeDebugResetToken;

        var result = await sender.Send(
            new ForgotPasswordCommand(request, includeDebugResetToken),
            cancellationToken);

        return Ok(result.Response);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetPasswordCommand(request), cancellationToken);

        if (result.ResultType == ResetPasswordCommandResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        return Ok();
    }

    private ActionResult<AuthResponse> ToAuthActionResult(AuthCommandResult result)
    {
        if (result.ResultType == AuthCommandResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == AuthCommandResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        if (result.ResultType == AuthCommandResultType.Unauthorized)
        {
            return Unauthorized(result.Error);
        }

        if (result.Response is null || result.RefreshToken is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Authentication operation failed.");
        }

        result.Response.RefreshToken = result.RefreshToken.Token;
        result.Response.RefreshTokenExpiresAtUtc = result.RefreshToken.ExpiresAtUtc;
        return Ok(result.Response);
    }
}
