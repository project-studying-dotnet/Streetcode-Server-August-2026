using System.Linq.Expressions;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Specifications.Streetcode.Streetcode;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Streetcode;

public class GetPublishedStreetcodesByIdsSpecificationTests
{
    [Fact]
    public void Evaluate_ReturnsOnlyRequestedStreetcodes()
    {
        var streetcodes = new List<StreetcodeContent>
        {
            new() { Id = 1, Status = StreetcodeStatus.Published },
            new() { Id = 2, Status = StreetcodeStatus.Published },
            new() { Id = 3, Status = StreetcodeStatus.Published },
        };

        var spec = new GetPublishedStreetcodesByIdsSpecification(new[] { 1, 3 });
        var result = spec.Evaluate(streetcodes).ToList();

        Assert.Equal(new[] { 1, 3 }, result.Select(sc => sc.Id));
    }

    [Theory]
    [InlineData(StreetcodeStatus.Draft)]
    [InlineData(StreetcodeStatus.Deleted)]
    public void Evaluate_ExcludesStreetcodesThatAreNotPublished(StreetcodeStatus status)
    {
        var streetcodes = new List<StreetcodeContent>
        {
            new() { Id = 1, Status = StreetcodeStatus.Published },
            new() { Id = 2, Status = status },
        };

        var spec = new GetPublishedStreetcodesByIdsSpecification(new[] { 1, 2 });
        var result = spec.Evaluate(streetcodes).ToList();

        var streetcode = Assert.Single(result);
        Assert.Equal(1, streetcode.Id);
    }

    [Fact]
    public void Evaluate_ReturnsNothing_WhenIdsAreEmpty()
    {
        var streetcodes = new List<StreetcodeContent>
        {
            new() { Id = 1, Status = StreetcodeStatus.Published },
        };

        var spec = new GetPublishedStreetcodesByIdsSpecification(Array.Empty<int>());
        var result = spec.Evaluate(streetcodes).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Spec_IncludesOnlyTags()
    {
        var spec = new GetPublishedStreetcodesByIdsSpecification(new[] { 1 });

        var includedProperties = spec.IncludeExpressions
            .Select(include => ((MemberExpression)include.LambdaExpression.Body).Member.Name)
            .ToList();

        Assert.Equal(new[] { nameof(StreetcodeContent.Tags) }, includedProperties);
    }
}
