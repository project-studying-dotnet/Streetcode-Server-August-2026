// <copyright file="CommentValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Validators;
    using Xunit;

    public class CommentValidatorsTests
    {
        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void GetCommentByIdQueryValidator_ShouldRequirePositiveId(
            int id,
            bool expectedIsValid)
        {
            var validator = new GetCommentByIdQueryValidator();

            var result = validator.Validate(new GetCommentByIdQuery(id));

            Assert.Equal(expectedIsValid, result.IsValid);
        }
    }
}
