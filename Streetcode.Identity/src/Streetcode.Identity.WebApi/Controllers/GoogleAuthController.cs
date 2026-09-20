using System.Globalization;
using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Streetcode.Identity.Application.Features.Authentication.Google;
using Streetcode.Identity.Application.Features.Authentication.Login;

namespace Streetcode.Identity.WebApi.Controllers;

public sealed record GoogleLoginRequest(string IdToken);
public sealed record LinkGoogleRequest(string IdToken, string Password, bool ConfirmLink);

[ApiController]
[Route("api/auth/google")]
[Consumes("application/json")]
public sealed class GoogleAuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GoogleLoginCommand(request.IdToken), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Failure(result.Errors);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Link(LinkGoogleRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !long.TryParse(User.FindFirstValue("access_version"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var version))
            return Unauthorized();

        var result = await sender.Send(new LinkGoogleCommand(request.IdToken, userId, version,
            request.Password, request.ConfirmLink), cancellationToken);
        return result.IsSuccess ? NoContent() : Failure(result.Errors);
    }

    private ObjectResult Failure(List<IError> errors)
    {
        var error = errors.First();
        var code = error.Metadata.GetValueOrDefault("Code") as string;
        var status = code switch
        {
            "Google.LinkRequired" or "Google.LinkConflict" => StatusCodes.Status409Conflict,
            "Google.InvalidToken" or "Google.Unauthorized" => StatusCodes.Status401Unauthorized,
            "Google.Unavailable" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };
        return StatusCode(status, new ProblemDetails
        {
            Status = status,
            Title = "Google authentication failed",
            Detail = status == 500 ? "An unexpected error occurred." : error.Message,
            Extensions = { ["code"] = code ?? "Google.Failed" },
        });
    }
}
