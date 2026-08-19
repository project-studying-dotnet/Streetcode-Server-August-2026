using System.ComponentModel.DataAnnotations;
using Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact;

public class FactUpdateCreateDtoValidationTests
{
    [Fact]
    public void Validate_WhenValuesAreWithinLimits_ShouldSucceed()
    {
        var dto = CreateValidDto();

        var validationResults = Validate(dto);

        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(nameof(FactUpdateCreateDto.Title), 69)]
    [InlineData(nameof(FactUpdateCreateDto.FactContent), 601)]
    [InlineData(nameof(FactUpdateCreateDto.ImageAlt), 201)]
    public void Validate_WhenTextExceedsLimit_ShouldReturnValidationError(
        string propertyName,
        int valueLength)
    {
        var dto = CreateValidDto();
        typeof(FactUpdateCreateDto)
            .GetProperty(propertyName)!
            .SetValue(dto, new string('a', valueLength));

        var validationResults = Validate(dto);

        Assert.Contains(
            validationResults,
            result => result.MemberNames.Contains(propertyName));
    }

    [Fact]
    public void Validate_WhenRequiredValuesAreInvalid_ShouldReturnValidationErrors()
    {
        var dto = new FactUpdateCreateDto
        {
            Title = string.Empty,
            FactContent = string.Empty,
            ImageId = 0,
            StreetcodeId = 0,
        };

        var invalidMembers = Validate(dto)
            .SelectMany(result => result.MemberNames)
            .ToHashSet();

        Assert.Contains(nameof(FactUpdateCreateDto.Title), invalidMembers);
        Assert.Contains(nameof(FactUpdateCreateDto.FactContent), invalidMembers);
        Assert.Contains(nameof(FactUpdateCreateDto.ImageId), invalidMembers);
        Assert.Contains(nameof(FactUpdateCreateDto.StreetcodeId), invalidMembers);
    }

    private static List<ValidationResult> Validate(FactUpdateCreateDto dto)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            validationResults,
            validateAllProperties: true);

        return validationResults;
    }

    private static FactUpdateCreateDto CreateValidDto() =>
        new()
        {
            Title = new string('a', 68),
            FactContent = new string('a', 600),
            ImageAlt = new string('a', 200),
            ImageId = 1,
            StreetcodeId = 1,
        };
}
