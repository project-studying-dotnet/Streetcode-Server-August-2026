using MediatR;
using Microsoft.AspNetCore.Mvc;
using Streetcode.Identity.Application.Features.Authentication.Logout;
using Streetcode.Identity.WebApi.Contracts.Authentication;

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
}
