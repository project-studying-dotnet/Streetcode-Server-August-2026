// <copyright file="DeleteSoftStreetcodeCommandValidatorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.DeleteSoft;
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;
    using Xunit;

    public class DeleteSoftStreetcodeCommandValidatorTests
    {
        [Fact]
        public void Validate_WhenIdIsPositive_ShouldBeValid()
        {
            var validator = new DeleteSoftStreetcodeCommandValidator();

            var result = validator.Validate(new DeleteSoftStreetcodeCommand(1));

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WhenIdIsNotPositive_ShouldBeInvalid(int id)
        {
            var validator = new DeleteSoftStreetcodeCommandValidator();

            var result = validator.Validate(new DeleteSoftStreetcodeCommand(id));

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(DeleteSoftStreetcodeCommand.Id));
        }
    }
}
