using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.MediatR.Newss.Create;
using Streetcode.BLL.MediatR.Newss.Delete;
using Streetcode.BLL.MediatR.Newss.GetAll;
using Streetcode.BLL.MediatR.Newss.GetById;
using Streetcode.BLL.MediatR.Newss.GetByUrl;
using Streetcode.BLL.MediatR.Newss.GetNewsAndLinksByUrl;
using Streetcode.BLL.MediatR.Newss.SortedByDateTime;
using Streetcode.BLL.MediatR.Newss.Update;

namespace Streetcode.WebApi.Controllers.News
{
    public class NewsController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return HandleResult(await Mediator.Send(new GetAllNewsQuery()));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return HandleResult(await Mediator.Send(new GetNewsByIdQuery(id)));
        }

        [HttpGet("by-url")]
        public async Task<IActionResult> GetByUrl([FromQuery] string url)
        {
            return HandleResult(await Mediator.Send(new GetNewsByUrlQuery(url)));
        }

        [HttpGet("sorted-by-date")]
        public async Task<IActionResult> GetSortedByDateTime()
        {
            return HandleResult(await Mediator.Send(new SortedByDateTimeQuery()));
        }

        [HttpGet("by-url-with-links")]
        public async Task<IActionResult> GetNewsAndLinks([FromQuery] string url)
        {
            return HandleResult(await Mediator.Send(new GetNewsAndLinksByUrlQuery(url)));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateNewsCommand command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        [HttpPut]
        public async Task<IActionResult> Update(UpdateNewsCommand command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return HandleResult(await Mediator.Send(new DeleteNewsCommand(id)));
        }
    }
}
