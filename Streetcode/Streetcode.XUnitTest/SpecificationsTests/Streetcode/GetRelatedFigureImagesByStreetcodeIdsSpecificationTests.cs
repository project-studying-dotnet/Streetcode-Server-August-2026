using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Enums;
using Streetcode.DAL.Specifications.Streetcode.Streetcode;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Streetcode;

public class GetRelatedFigureImagesByStreetcodeIdsSpecificationTests
{
    [Fact]
    public void Evaluate_KeepsRelatedFigureAndUnassignedImages_AndDropsOtherAssignments()
    {
        var streetcodeImages = new List<StreetcodeImage>
        {
            new() { StreetcodeId = 1, ImageId = 10, ImageAssignment = ImageAssignment.Animation },
            new() { StreetcodeId = 1, ImageId = 11, ImageAssignment = ImageAssignment.BlackAndWhite },
            new() { StreetcodeId = 1, ImageId = 12, ImageAssignment = ImageAssignment.RelatedFigure },
            new() { StreetcodeId = 1, ImageId = 13, ImageAssignment = null },
        };

        var spec = new GetRelatedFigureImagesByStreetcodeIdsSpecification(new[] { 1 });
        var result = spec.Evaluate(streetcodeImages).ToList();

        Assert.Equal(new[] { 12, 13 }, result.Select(si => si.ImageId).OrderBy(id => id));
    }

    [Fact]
    public void Evaluate_ReturnsOnlyImagesOfRequestedStreetcodes()
    {
        var streetcodeImages = new List<StreetcodeImage>
        {
            new() { StreetcodeId = 1, ImageId = 10, ImageAssignment = ImageAssignment.RelatedFigure },
            new() { StreetcodeId = 2, ImageId = 20, ImageAssignment = ImageAssignment.RelatedFigure },
            new() { StreetcodeId = 3, ImageId = 30, ImageAssignment = ImageAssignment.RelatedFigure },
        };

        var spec = new GetRelatedFigureImagesByStreetcodeIdsSpecification(new[] { 1, 3 });
        var result = spec.Evaluate(streetcodeImages).ToList();

        Assert.Equal(new[] { 1, 3 }, result.Select(si => si.StreetcodeId));
    }

    [Fact]
    public void Evaluate_ReturnsNothing_WhenIdsAreEmpty()
    {
        var streetcodeImages = new List<StreetcodeImage>
        {
            new() { StreetcodeId = 1, ImageId = 10, ImageAssignment = ImageAssignment.RelatedFigure },
        };

        var spec = new GetRelatedFigureImagesByStreetcodeIdsSpecification(Array.Empty<int>());
        var result = spec.Evaluate(streetcodeImages).ToList();

        Assert.Empty(result);
    }
}
