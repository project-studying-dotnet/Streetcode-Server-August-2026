using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Email;
using Streetcode.BLL.MediatR.Email;

namespace Streetcode.WebApi.Controllers.Email
{
    public class EmailController : BaseApiController
    {
        [HttpPost]
        public async Task<IActionResult> Send(
            [FromBody] EmailDTO email,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new SendEmailCommand(email),
                cancellationToken);

            if (result.IsFailed)
            {
                return HandleResult(result);
            }

            return Accepted(new { MessageId = result.Value });
        }
    }
}
