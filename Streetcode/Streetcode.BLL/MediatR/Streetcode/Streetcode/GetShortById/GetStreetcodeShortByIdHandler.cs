using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.GetShortById
{
    public class GetStreetcodeShortByIdHandler : IRequestHandler<GetStreetcodeShortByIdQuery, Result<StreetcodeShortDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerService _logger;
        private readonly ICacheService _cacheService;

        public GetStreetcodeShortByIdHandler(IMapper mapper, IRepositoryWrapper repository, ILoggerService logger, ICacheService cacheService)
        {
            _mapper = mapper;
            _repository = repository;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<StreetcodeShortDTO>> Handle(GetStreetcodeShortByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"streetcode:short:{request.id}";
            var streetcodeNotFound = false;

            var streetcodeShortDto = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async (ct) =>
                {
                    var streetcode = await _repository.StreetcodeRepository
                                        .GetFirstOrDefaultAsync(st => st.Id == request.id);

                    if(streetcode is null)
                    {
                        streetcodeNotFound = true;
                        return null;
                    }

                    return _mapper.Map<StreetcodeShortDTO>(streetcode);
                },
                cancellationToken: cancellationToken);

            if(streetcodeShortDto == null)
            {
                if (streetcodeNotFound)
                {
                    const string notFoundMsg = "Cannot find streetcode by id";
                    _logger.LogError(request, notFoundMsg);
                    return Result.Fail(new Error(notFoundMsg));
                }

                const string mapErrorMsg = "Cannot map streetcode to shortDTO";
                _logger.LogError(request, mapErrorMsg);
                return Result.Fail(new Error(mapErrorMsg));
            }

            return Result.Ok(streetcodeShortDto);
        }
    }
}
