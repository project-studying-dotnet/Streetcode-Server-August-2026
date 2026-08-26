using MediatR;
using Microsoft.AspNetCore.Mvc;
using Streetcode.Identity.Application.Features.Registration;
using Streetcode.Identity.WebApi.DTOs;

namespace Streetcode.Identity.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.Password,
                request.PhoneNumber
            );

            var result = await _mediator.Send(command, cancellationToken);

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
    }
}