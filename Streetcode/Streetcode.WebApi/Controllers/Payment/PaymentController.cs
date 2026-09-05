using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.Payment;
using Streetcode.BLL.MediatR.Payment;
using Streetcode.DAL.Enums;
using Streetcode.WebApi.Attributes;

namespace Streetcode.WebApi.Controllers.Payment
{
    public class PaymentController : BaseApiController
    {
        [AuthorizeRoles(UserRole.MainAdministrator, UserRole.Administrator)]
        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] PaymentDTO payment)
        {
            return HandleResult(await Mediator.Send(new CreateInvoiceCommand(payment)));
        }
    }
}
