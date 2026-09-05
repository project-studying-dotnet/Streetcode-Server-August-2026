using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Specifications.Partners;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Partners;

public class GetAllPartnersSpecificationTests
{
    [Fact]
    public void Evaluate_ReturnsAllPartners()
    {
        var partners = new List<Partner>
            {
                new() { Id = 1},
                new() { Id = 2},
                new() { Id = 3},
            };

        var spec = new GetAllPartnersSpecification();
        var result = spec.Evaluate(partners).ToList();

        Assert.Equal(3 , result.Count);
        Assert.Equal(partners.Select(x => x.Id), result.Select(x => x.Id));
    }
}
