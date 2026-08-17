using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Streetcode.Types;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Enums;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Create
{
    public class CreateStreetcodeHandler : IRequestHandler<CreateStreetcodeCommand, Result<StreetcodeDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerService _logger;

        public CreateStreetcodeHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<StreetcodeDTO>> Handle(CreateStreetcodeCommand request, CancellationToken cancellationToken)
        {
            var dto = request.newStreetcode;
            StreetcodeContent entity = dto.StreetcodeType == StreetcodeType.Person
                ? new PersonStreetcode()
                : new EventStreetcode();

            try
            {
                _mapper.Map(dto, entity);

                var tagIds = dto.Tags?.Select(t => t.Id).ToList() ?? new List<int>();
                var existingTags = await _repositoryWrapper.TagRepository.GetAllAsync(t => tagIds.Contains(t.Id));
                entity.Tags.AddRange(existingTags);

                await _repositoryWrapper.StreetcodeRepository.CreateAsync(entity);
                var success = await _repositoryWrapper.SaveChangesAsync() > 0;

                if (!success)
                {
                    const string errorMsg = "Failed to create streetcode";
                    _logger.LogError(request, errorMsg);
                    return Result.Fail(new Error(errorMsg));
                }

                return Result.Ok(_mapper.Map<StreetcodeDTO>(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(request, ex.Message);
                return Result.Fail(ex.Message);
            }
        }
    }
}
