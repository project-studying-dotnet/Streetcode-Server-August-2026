using Ardalis.Specification;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Enums;

namespace Streetcode.DAL.Specifications.Streetcode.Streetcode;

public class GetRelatedFigureImagesByStreetcodeIdsSpecification : Specification<StreetcodeImage>
{
    public GetRelatedFigureImagesByStreetcodeIdsSpecification(IReadOnlyCollection<int> streetcodeIds)
    {
        Query
            .Where(si => streetcodeIds.Contains(si.StreetcodeId)
                && (si.ImageAssignment == ImageAssignment.RelatedFigure || si.ImageAssignment == null));
    }
}
