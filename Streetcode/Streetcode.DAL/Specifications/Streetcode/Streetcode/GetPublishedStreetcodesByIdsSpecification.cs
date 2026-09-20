using Ardalis.Specification;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Enums;

namespace Streetcode.DAL.Specifications.Streetcode.Streetcode;

public class GetPublishedStreetcodesByIdsSpecification : Specification<StreetcodeContent>
{
    public GetPublishedStreetcodesByIdsSpecification(IReadOnlyCollection<int> ids)
    {
        Query
            .Where(sc => sc.Status == StreetcodeStatus.Published && ids.Contains(sc.Id))
            .Include(sc => sc.Tags);
    }
}
