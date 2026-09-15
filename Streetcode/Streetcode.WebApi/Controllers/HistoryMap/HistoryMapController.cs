using Microsoft.AspNetCore.Mvc;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.MediatR.HistoryMap.Create;
using Streetcode.BLL.MediatR.HistoryMap.Delete;
using Streetcode.BLL.MediatR.HistoryMap.GetByStreetcode;
using Streetcode.BLL.MediatR.HistoryMap.Merge;

namespace Streetcode.WebApi.Controllers.HistoryMap
{
    public class HistoryMapController : BaseApiController
    {
        [HttpGet("{streetcodeId:int}")]
        public async Task<IActionResult> GetByStreetcodeId([FromRoute] int streetcodeId)
        {
            return HandleResult(await Mediator.Send(new GetHistoryMapRecordsByStreetcodeQuery(streetcodeId)));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHistoryMapRecordDTO dto)
        {
            return HandleResult(await Mediator.Send(new CreateHistoryMapRecordCommand(dto)));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return HandleResult(await Mediator.Send(new DeleteHistoryMapRecordCommand(id)));
        }

        [HttpPost("merge-toponyms")]
        public async Task<IActionResult> MergeToponyms([FromBody] MergeToponymsDTO dto)
        {
            return HandleResult(await Mediator.Send(new MergeToponymsCommand(dto)));
        }
    }
}