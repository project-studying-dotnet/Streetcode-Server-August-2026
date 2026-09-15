using Ardalis.Specification;
using Streetcode.DAL.Entities.Team;

namespace Streetcode.DAL.Specifications.Team
{
    public class GetAllMainTeamSpecification : Specification<TeamMember>
    {
        public GetAllMainTeamSpecification()
        {
            Query
                .Where(x => x.IsMain)
                .Include(x => x.Positions)
                .Include(x => x.TeamMemberLinks);
        }
    }
}
