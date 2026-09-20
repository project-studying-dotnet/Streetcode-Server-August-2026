using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIds;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;
using Streetcode.BLL.MediatR.Validators;
using Xunit;

namespace Streetcode.XUnitTest.ValidatorTests;

public class GetStreetcodesByIdsQueryValidatorTests
{
    private readonly GetStreetcodesByIdsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdsArePositive_ShouldBeValid()
    {
        var result = _validator.Validate(new GetStreetcodesByIdsQuery(new[] { 1, 2, 3 }));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_ShouldBeValid()
    {
        var result = _validator.Validate(new GetStreetcodesByIdsQuery(Array.Empty<int>()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsCountEqualsLimit_ShouldBeValid()
    {
        var ids = Enumerable.Range(1, PaginationLimits.MaxPageSize).ToArray();

        var result = _validator.Validate(new GetStreetcodesByIdsQuery(ids));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsCountExceedsLimit_ShouldBeInvalid()
    {
        var ids = Enumerable.Range(1, PaginationLimits.MaxPageSize + 1).ToArray();

        var result = _validator.Validate(new GetStreetcodesByIdsQuery(ids));

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetStreetcodesByIdsQuery.Ids));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenAnyIdIsNotPositive_ShouldBeInvalid(int invalidId)
    {
        var result = _validator.Validate(new GetStreetcodesByIdsQuery(new[] { 5, invalidId }));

        var error = Assert.Single(result.Errors);
        Assert.Equal("Ids[1]", error.PropertyName);
    }
}
