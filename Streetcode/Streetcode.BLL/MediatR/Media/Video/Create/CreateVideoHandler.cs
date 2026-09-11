using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Media.Video;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Media.Video.Create;

public class CreateVideoHandler : IRequestHandler<CreateVideoCommand, Result<VideoDTO>>
{
    private readonly IMapper _mapper;
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;

    public CreateVideoHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ILoggerService logger)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<VideoDTO>> Handle(
        CreateVideoCommand request,
        CancellationToken cancellationToken)
    {
        var video = _mapper.Map<DAL.Entities.Media.Video>(request.Video);

        await _repositoryWrapper.VideoRepository.CreateAsync(video);

        var resultIsSuccess = await _repositoryWrapper.SaveChangesAsync() > 0;

        if (!resultIsSuccess)
        {
            const string errorMsg = "Failed to create a video";
            _logger.LogError(request, errorMsg);
            return Result.Fail(new Error(errorMsg));
        }

        return Result.Ok(_mapper.Map<VideoDTO>(video));
    }
}