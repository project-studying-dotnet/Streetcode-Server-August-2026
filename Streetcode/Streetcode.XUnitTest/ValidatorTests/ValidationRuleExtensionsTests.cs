// <copyright file="ValidationRuleExtensionsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ValidatorTests
{
    using FluentValidation;
    using Streetcode.BLL.MediatR.Validators;
    using Xunit;

    public class ValidationRuleExtensionsTests
    {
        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void MustBeValidId_ShouldRequirePositiveValue(
            int id,
            bool expectedIsValid)
        {
            var validator = new InlineValidator<TestModel>();
            validator.RuleFor(model => model.Id)
                .MustBeValidId("Test");

            var result = validator.Validate(new TestModel { Id = id });

            Assert.Equal(expectedIsValid, result.IsValid);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("https://streetcode.com", true)]
        [InlineData("http://streetcode.com/path", true)]
        [InlineData("/relative/path", false)]
        [InlineData("ftp://streetcode.com", false)]
        [InlineData("not a url", false)]
        public void MustBeValidHttpUrl_ShouldAllowOnlyAbsoluteHttpUrls(
            string? url,
            bool expectedIsValid)
        {
            var validator = new InlineValidator<TestModel>();
            validator.RuleFor(model => model.Text)
                .MustBeValidHttpUrl("URL");

            var result = validator.Validate(new TestModel { Text = url });

            Assert.Equal(expectedIsValid, result.IsValid);
        }

        [Theory]
        [InlineData("12345", true)]
        [InlineData("123456", false)]
        public void MustNotExceedLength_ShouldEnforceMaximumLength(
            string text,
            bool expectedIsValid)
        {
            var validator = new InlineValidator<TestModel>();
            validator.RuleFor(model => model.Text)
                .MustNotExceedLength(5, "Text");

            var result = validator.Validate(new TestModel { Text = text });

            Assert.Equal(expectedIsValid, result.IsValid);
        }

        private sealed class TestModel
        {
            public int Id { get; set; }

            public string? Text { get; set; }
        }
    }
}
