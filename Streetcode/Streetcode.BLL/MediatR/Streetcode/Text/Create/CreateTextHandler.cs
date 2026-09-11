using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.BLL.Constants;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Create;

public class CreateTextHandler : IRequestHandler<CreateTextCommand, Result<TextDTO>>
{
    private readonly IMapper _mapper;
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;

    public CreateTextHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<TextDTO>> Handle(
        CreateTextCommand request,
        CancellationToken cancellationToken)
    {
        var text = _mapper.Map<DAL.Entities.Streetcode.TextContent.Text>(request.Text);

        if (text.AdditionalText == TextConstants.DefaultAdditionalText)
        {
            text.AdditionalText = null;
        }

        text.StreetcodeId = request.StreetcodeId;

        await _repositoryWrapper.TextRepository.CreateAsync(text);

        var resultIsSuccess = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!resultIsSuccess)
        {
            const string errorMsg = "Failed to create a text";
            _logger.LogError(request, errorMsg);
            return Result.Fail(new Error(errorMsg));
        }

        return Result.Ok(_mapper.Map<TextDTO>(text));
    }
}