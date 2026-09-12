using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByTransliterationUrl
{
  public class GetStreetcodeByTransliterationUrlHandler : IRequestHandler<GetStreetcodeByTransliterationUrlQuery, Result<StreetcodeDTO>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;
        private readonly ILoggerService _logger;
        private readonly ICacheService _cacheService;

        public GetStreetcodeByTransliterationUrlHandler(IRepositoryWrapper repository, IMapper mapper, ILoggerService logger, ICacheService cacheService)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<StreetcodeDTO>> Handle(GetStreetcodeByTransliterationUrlQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"streetcode:url:{request.url}";
            var streetcodeDto = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async (ct) =>
                {
                    var streetcode = await _repository.StreetcodeRepository.GetFirstOrDefaultAsync(
                                    predicate: st => st.TransliterationUrl == request.url);

                    if (streetcode is null)
                    {
                        return null;
                    }

                    var tagIndexed = await _repository.StreetcodeTagIndexRepository
                                            .GetAllAsync(
                                                t => t.StreetcodeId == streetcode.Id,
                                                include: q => q.Include(ti => ti.Tag));

                    var dto = _mapper.Map<StreetcodeDTO>(streetcode);
                    dto.Tags = _mapper.Map<List<StreetcodeTagDTO>>(tagIndexed);

                    return dto;
                },
                cancellationToken: cancellationToken);

            if (streetcodeDto == null)
            {
                string errorMsg = $"Cannot find streetcode by transliteration url: {request.url}";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }

            return Result.Ok(streetcodeDto);
        }
    }
}
