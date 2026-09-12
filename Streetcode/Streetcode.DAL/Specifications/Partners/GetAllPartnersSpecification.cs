using Ardalis.Specification;
using Streetcode.DAL.Entities.Partners;

namespace Streetcode.DAL.Specifications.Partners
{
    public class GetAllPartnersSpecification : Specification<Partner>
    {
        public GetAllPartnersSpecification()
        {
            Query
                .Include(pl => pl.PartnerSourceLinks)
                .Include(p => p.Streetcodes);
        }
    }
}
