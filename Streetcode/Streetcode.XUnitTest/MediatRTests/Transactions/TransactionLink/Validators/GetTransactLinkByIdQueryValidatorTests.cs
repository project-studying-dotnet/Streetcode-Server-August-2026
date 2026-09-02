namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.Validators
{
    using FluentValidation.TestHelper;
    using global::Streetcode.BLL.MediatR.Transactions.TransactionLink.GetById;
    using global::Streetcode.BLL.MediatR.Transactions.TransactionLink.Validators;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xunit;

    public class GetTransactLinkByIdQueryValidatorTests
    {
        private readonly GetTransactLinkByIdQueryValidator validator;

        public GetTransactLinkByIdQueryValidatorTests()
        {
            this.validator = new GetTransactLinkByIdQueryValidator();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void Validate_ShouldNotHaveError_WhenIdIsValid(int id)
        {
            var query = new GetTransactLinkByIdQuery(id);

            var result = this.validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(q => q.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Validate_ShouldHaveError_WhenIdIsInvalid(int id)
        {
            var query = new GetTransactLinkByIdQuery(id);

            var result = this.validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(q => q.Id);
        }
    }
}
