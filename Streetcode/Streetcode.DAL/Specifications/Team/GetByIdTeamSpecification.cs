using Ardalis.Specification;
using Streetcode.DAL.Entities.Team;

namespace Streetcode.DAL.Specifications.Team
{
    public class GetByIdTeamSpecification : Specification<TeamMember>
    {
        public GetByIdTeamSpecification(int id)
        {
            Query
                .Where(x => x.Id == id)
                .Include(x => x.TeamMemberLinks)
                .Include(x => x.Positions);
        }
    }
}
