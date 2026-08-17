using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Specifications.Partners;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Partners;

public class GetPartnersByStreetcodeIdSpecificationTests
{
    [Fact]
    public void Evaluate_ReturnsPartner_WhenLinkedToStreetcode()
    {
        var partners = new List<Partner>
            {
                new()
                {
                    Id = 1,
                    Title = "Linked Partner",
                    IsVisibleEverywhere = false,
                    Streetcodes = new List<StreetcodeContent> { new() { Id = 42 } },
                },
                new()
                {
                    Id = 2,
                    Title = "Unrelated Partner",
                    IsVisibleEverywhere = false,
                    Streetcodes = new List<StreetcodeContent> { new() { Id = 99 } },
                },
            };

        var spec = new GetPartnersByStreetcodeIdSpecification(42);
        var result = spec.Evaluate(partners).ToList();

        var partner = Assert.Single(result);
        Assert.Equal(1, partner.Id);
    }

    [Fact]
    public void Evaluate_ReturnsPartner_WhenVisibleEverywhere_EvenWithoutLink()
    {
        var partners = new List<Partner>
            {
                new()
                {
                    Id = 1,
                    Title = "Visible Everywhere Partner",
                    IsVisibleEverywhere = true,
                    Streetcodes = new List<StreetcodeContent>(),
                },
            };

        var spec = new GetPartnersByStreetcodeIdSpecification(42);
        var result = spec.Evaluate(partners).ToList();

        var partner = Assert.Single(result);
        Assert.Equal(1, partner.Id);
    }

    [Fact]
    public void Evaluate_ExcludesPartner_WhenNotLinkedAndNotVisibleEverywhere()
    {
        var partners = new List<Partner>
            {
                new()
                {
                    Id = 1,
                    Title = "Excluded Partner",
                    IsVisibleEverywhere = false,
                    Streetcodes = new List<StreetcodeContent> { new() { Id = 99 } },
                },
            };

        var spec = new GetPartnersByStreetcodeIdSpecification(42);
        var result = spec.Evaluate(partners).ToList();

        Assert.Empty(result);
    }
}
