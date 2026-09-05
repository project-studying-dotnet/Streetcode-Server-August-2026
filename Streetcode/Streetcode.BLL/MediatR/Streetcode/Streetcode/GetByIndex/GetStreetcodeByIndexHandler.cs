using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.Interfaces.CacheService;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Realizations.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIndex;

public class GetStreetcodeByIndexHandler : IRequestHandler<GetStreetcodeByIndexQuery, Result<StreetcodeDTO>>
{
    private readonly IMapper _mapper;
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly ILoggerService _logger;
    private readonly ICacheService _cacheService;

    public GetStreetcodeByIndexHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper, ILoggerService logger, ICacheService cacheService)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<StreetcodeDTO>> Handle(GetStreetcodeByIndexQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"streetcode:index:{request.Index}";

        var streetcodeDto = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async (ct) =>
            {
                var streetcode = await _repositoryWrapper.StreetcodeRepository.GetFirstOrDefaultAsync(
                        predicate: st => st.Index == request.Index,
                        include: source => source.Include(l => l.Tags));

                return streetcode is not null ? _mapper.Map<StreetcodeDTO>(streetcode) : null;
            },
            cancellationToken: cancellationToken);

        if (streetcodeDto is null)
        {
            string errorMsg = $"Cannot find any streetcode with corresponding index: {request.Index}";
            _logger.LogError(request, errorMsg);
            return Result.Fail(new Error(errorMsg));
        }

        return Result.Ok(streetcodeDto);
    }
}