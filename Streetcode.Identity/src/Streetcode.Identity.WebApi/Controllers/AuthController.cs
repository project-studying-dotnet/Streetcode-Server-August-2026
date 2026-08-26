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
}