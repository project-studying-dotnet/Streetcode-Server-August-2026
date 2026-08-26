using MediatR;
using Microsoft.AspNetCore.Mvc;
using Streetcode.Identity.Application.Features.Authentication.Logout;
using Streetcode.Identity.Application.Features.Registration;
using Streetcode.Identity.WebApi.Contracts.Authentication;
using Streetcode.Identity.WebApi.DTOs;

namespace Streetcode.Identity.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISender _sender;

    public AuthController(IMediator mediator, ISender sender)
    {
        _mediator = mediator;
        _sender = sender;
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LogoutCommand(request.RefreshToken),
            cancellationToken);

        if (result.IsFailed)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to terminate the session.");
        }

        return NoContent();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Password,
            request.BirthDate,
            request.Phone,
            request.Gender
        );

        var result = await _mediator.Send(command);

        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result);
    }
}