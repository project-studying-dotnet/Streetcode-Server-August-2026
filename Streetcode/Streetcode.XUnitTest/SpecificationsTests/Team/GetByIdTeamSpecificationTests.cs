using Streetcode.DAL.Entities.Team;
using Streetcode.DAL.Specifications.Team;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Team;

public class GetByIdTeamSpecificationTests
{
    [Fact]
    public void Evaluate_ReturnMemberById()
    {
        var members = new List<TeamMember>
        {
            new() { Id = 1, IsMain = true },
            new() { Id = 2, IsMain = false },
            new() { Id = 3, IsMain = true },
        };

        var spec = new GetByIdTeamSpecification(2);
        var result = spec.Evaluate(members);

        var member = Assert.Single(result);
        Assert.Equal(2, member.Id);
    }

    [Fact]
    public void Evaluate_ReturnEmpty_WhenIdNotFound()
    {
        var members = new List<TeamMember>
        {
            new() { Id = 1, IsMain = true },
            new() { Id = 2, IsMain = false },
            new() { Id = 3, IsMain = true },
        };

        var spec = new GetByIdTeamSpecification(999);
        var result = spec.Evaluate(members);

        Assert.Empty(result);
    }
}
