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
            var streetcodeShortDto = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async (ct) =>
                {
                    var streetcode = await _repository.StreetcodeRepository
                                        .GetFirstOrDefaultAsync(st => st.Id == request.id);

                    return streetcode is not null ? _mapper.Map<StreetcodeShortDTO>(streetcode) : null;
                },
                cancellationToken: cancellationToken);

            if(streetcodeShortDto == null)
            {
                const string errorMsg = "Cannot map streetcode to shortDTO";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }

            return Result.Ok(streetcodeShortDto);
        }
    }
}
