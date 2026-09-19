using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.RelatedFigure;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Specifications.Streetcode.Streetcode;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIds;

public class GetStreetcodesByIdsHandler : IRequestHandler<GetStreetcodesByIdsQuery, Result<IEnumerable<RelatedFigureDTO>>>
{
    private readonly IMapper _mapper;
    private readonly IRepositoryWrapper _repositoryWrapper;

    public GetStreetcodesByIdsHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<RelatedFigureDTO>>> Handle(GetStreetcodesByIdsQuery request, CancellationToken cancellationToken)
    {
        var ids = request.Ids.Distinct().ToList();
        var specification = new GetPublishedStreetcodesByIdsSpecification(ids);
        var streetcodes = await _repositoryWrapper.StreetcodeRepository
            .ListAsync(specification, cancellationToken);

        // Unknown and unpublished ids are left out, the rest keep the requested order.
        var streetcodesById = streetcodes.ToDictionary(streetcode => streetcode.Id);
        var orderedStreetcodes = ids
            .Where(streetcodesById.ContainsKey)
            .Select(id => streetcodesById[id]);

        return Result.Ok(_mapper.Map<IEnumerable<RelatedFigureDTO>>(orderedStreetcodes));
    }
}
