using MediatR;
using Microsoft.AspNetCore.Mvc;
using Streetcode.Identity.Application.Features.Authentication.Refresh;
using Streetcode.Identity.Application.Features.Registration;
using Streetcode.Identity.WebApi.DTOs;

namespace Streetcode.Identity.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Password,
            request.PhoneNumber
        );

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Registration failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = result.Errors.FirstOrDefault()?.Message
            });
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshSessionCommand(request.RefreshToken);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Title = "Refresh session failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "The refresh token is invalid or inactive"
            });
        }

        return Ok(result.Value);
    }
}
