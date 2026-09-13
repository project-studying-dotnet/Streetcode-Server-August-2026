// <copyright file="CommentValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Streetcode.Comments;
    using Streetcode.BLL.MediatR.Streetcode.Comment.GetAll;
    using Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Validators;
    using Streetcode.BLL.MediatR.Validators;
    using Xunit;

    public class CommentValidatorsTests
    {
        [Fact]
        public void GetCommentsToReviewQueryValidator_WhenRequestIsValid_ShouldBeValid()
        {
            var validator = CreateGetCommentsToReviewQueryValidator();
            var query = new GetCommentsToReviewQuery(
                new GetCommentsToReviewRequestDto());

            var result = validator.Validate(query);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void GetCommentsToReviewQueryValidator_WhenRequestIsNull_ShouldBeInvalid()
        {
            var validator = CreateGetCommentsToReviewQueryValidator();
            var query = new GetCommentsToReviewQuery(null!);

            var result = validator.Validate(query);

            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(GetCommentsToReviewQuery.Request));
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        [InlineData(1, PaginationLimits.MaxPageSize + 1)]
        public void GetCommentsToReviewRequestDtoValidator_WhenPaginationIsInvalid_ShouldBeInvalid(
            int page,
            int amount)
        {
            var validator = new GetCommentsToReviewRequestDtoValidator();
            var request = new GetCommentsToReviewRequestDto
            {
                Page = page,
                Amount = amount,
            };

            var result = validator.Validate(request);

            Assert.False(result.IsValid);
        }

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

        private static GetCommentsToReviewQueryValidator CreateGetCommentsToReviewQueryValidator()
        {
            return new GetCommentsToReviewQueryValidator(
                new GetCommentsToReviewRequestDtoValidator());
        }
    }
}
