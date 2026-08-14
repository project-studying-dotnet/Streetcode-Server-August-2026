using Ardalis.Specification;
using Streetcode.DAL.Entities.Team;

namespace Streetcode.DAL.Specifications.Team
{
    public class GetAllTeamSpecification : Specification<TeamMember>
    {
        public GetAllTeamSpecification()
        {
            Query
                .Include(x => x.Positions)
                .Include(x => x.TeamMemberLinks);
        }
    }
}
