namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.Validators
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FluentValidation.TestHelper;
    using global::Streetcode.BLL.MediatR.Transactions.TransactionLink.GetByStreetcodeId;
    using global::Streetcode.BLL.MediatR.Transactions.TransactionLink.Validators;
    using Xunit;

    public class GetTransactLinkByStreetcodeIdQueryValidatorTests
    {
        private readonly GetTransactLinkByStreetcodeIdQueryValidator validator;

        public GetTransactLinkByStreetcodeIdQueryValidatorTests()
        {
            this.validator = new GetTransactLinkByStreetcodeIdQueryValidator();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void Validate_ShouldNotHaveError_WhenStreetcodeIdIsValid(int streetcodeId)
        {
            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = this.validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(q => q.StreetcodeId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Validate_ShouldHaveError_WhenStreetcodeIdIsInvalid(int streetcodeId)
        {
            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = this.validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(q => q.StreetcodeId);
        }
    }
}
