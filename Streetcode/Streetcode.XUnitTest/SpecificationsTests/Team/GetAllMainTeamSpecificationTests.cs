using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Specifications.Team;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Team;

public class GetAllMainTeamSpecificationTests
{
    [Fact]
    public void Evaluate_ReturnsOnlyMainMembers()
    {
        var members = new List<TeamMember>
        {
            new() { Id = 1, IsMain = true },
            new() { Id = 2, IsMain = false },
            new() { Id = 3, IsMain = true },
        };

        var spec = new GetAllMainTeamSpecification();
        var result = spec.Evaluate(members).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.True(m.IsMain));
    }
}
