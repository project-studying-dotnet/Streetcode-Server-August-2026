// <copyright file="StreetcodeFilterAndSortValidatorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Streetcode;
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.Filters;
    using Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;
    using Streetcode.DAL.Enums;
    using Xunit;

    public class StreetcodeFilterAndSortValidatorTests
    {
        [Theory]
        [InlineData("status:Draft", StreetcodeStatus.Draft)]
        [InlineData("STATUS:published", StreetcodeStatus.Published)]
        [InlineData(" status : Deleted ", StreetcodeStatus.Deleted)]
        public void TryParse_WhenFilterIsSupported_ShouldReturnStatus(
            string filter,
            StreetcodeStatus expectedStatus)
        {
            bool parsed = StreetcodeFilterParser.TryParse(
                filter,
                out StreetcodeStatus status);

            Assert.True(parsed);
            Assert.Equal(expectedStatus, status);
        }

        [Theory]
        [InlineData("anything:Published")]
        [InlineData("status:")]
        [InlineData("status:Published:extra")]
        [InlineData("status:Unknown")]
        [InlineData("status:1")]
        [InlineData("")]
        public void TryParse_WhenFilterIsUnsupported_ShouldReturnFalse(
            string filter)
        {
            bool parsed = StreetcodeFilterParser.TryParse(filter, out _);

            Assert.False(parsed);
        }

        [Theory]
        [InlineData("status:Published", true)]
        [InlineData("anything:Published", false)]
        [InlineData("status:", false)]
        [InlineData("status:Published:extra", false)]
        public void Validate_WhenFilterIsProvided_ShouldMatchParserContract(
            string filter,
            bool expectedIsValid)
        {
            var validator = new GetAllStreetcodesRequestDtoValidator();
            var request = new GetAllStreetcodesRequestDTO
            {
                Filter = filter,
            };

            var result = validator.Validate(request);

            Assert.Equal(expectedIsValid, result.IsValid);
        }

        [Theory]
        [InlineData("Title", true)]
        [InlineData("-Title", true)]
        [InlineData("title", true)]
        [InlineData("Unknown", false)]
        [InlineData("-", false)]
        [InlineData("", false)]
        public void Validate_WhenSortIsProvided_ShouldAllowOnlyStreetcodeProperties(
            string sort,
            bool expectedIsValid)
        {
            var validator = new GetAllStreetcodesRequestDtoValidator();
            var request = new GetAllStreetcodesRequestDTO
            {
                Sort = sort,
            };

            var result = validator.Validate(request);

            Assert.Equal(expectedIsValid, result.IsValid);
        }
    }
}
