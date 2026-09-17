using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Specifications.Team;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Team;

public class GetAllTeamSpecificationTests
{
    [Fact]
    public void Evaluate_ReturnsAllMembers()
    {
        var members = new List<TeamMember>
        {
            new() { Id = 1, IsMain = true },
            new() { Id = 2, IsMain = false },
            new() { Id = 3, IsMain = true },
        };

        var spec = new GetAllTeamSpecification();
        var result = spec.Evaluate(members).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(members.Select(m => m.Id), result.Select(r => r.Id));
    }
}
