using Ardalis.Specification;
using Streetcode.DAL.Entities.Partners;

namespace Streetcode.DAL.Specifications.Partners
{
    public class GetPartnerByIdSpecification : Specification<Partner>
    {
        public GetPartnerByIdSpecification(int id)
        {
            Query
                .Where(pl => pl.Id == id)
                .Include(pl => pl.PartnerSourceLinks);

        }
    }
}
