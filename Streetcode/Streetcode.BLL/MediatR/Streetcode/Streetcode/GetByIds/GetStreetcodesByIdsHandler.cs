using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.RelatedFigure;
using Streetcode.DAL.Enums;
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
        var streetcodes = await _repositoryWrapper.StreetcodeRepository
            .ListAsync(new GetPublishedStreetcodesByIdsSpecification(ids), cancellationToken);
        var streetcodeIds = streetcodes.Select(streetcode => streetcode.Id).ToList();
        var streetcodeImages = await _repositoryWrapper.StreetcodeImageRepository
            .ListAsync(new GetRelatedFigureImagesByStreetcodeIdsSpecification(streetcodeIds), cancellationToken);

        var imageIdByStreetcodeId = streetcodeImages
            .GroupBy(streetcodeImage => streetcodeImage.StreetcodeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(streetcodeImage => streetcodeImage.ImageAssignment == ImageAssignment.RelatedFigure)
                    .ThenByDescending(streetcodeImage => streetcodeImage.ImageId)
                    .First()
                    .ImageId);

        var streetcodesById = streetcodes.ToDictionary(streetcode => streetcode.Id);
        var orderedStreetcodes = ids
            .Where(streetcodesById.ContainsKey)
            .Select(id => streetcodesById[id]);

        var streetcodeDtos = _mapper.Map<IEnumerable<RelatedFigureDTO>>(orderedStreetcodes).ToList();
        foreach (var streetcodeDto in streetcodeDtos)
        {
            streetcodeDto.ImageId = imageIdByStreetcodeId.GetValueOrDefault(streetcodeDto.Id);
        }

        return Result.Ok<IEnumerable<RelatedFigureDTO>>(streetcodeDtos);
    }
}
