using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.RelatedFigure;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIds;

public record GetStreetcodesByIdsQuery(IReadOnlyCollection<int> Ids)
    : IRequest<Result<IEnumerable<RelatedFigureDTO>>>;
