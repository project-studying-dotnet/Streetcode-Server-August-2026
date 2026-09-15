// <copyright file="CreateReplyValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Streetcode.Comments;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Reply;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Validators;
    using Xunit;
    using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

    public class CreateReplyValidatorsTests
    {
        [Fact]
        public void CreateCommentDtoValidator_WhenTextIsValid_ShouldBeValid()
        {
            var result = new CreateCommentDtoValidator().Validate(
                new CreateCommentDto { Text = new string('a', CommentEntity.TextMaxLength) });

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CreateCommentDtoValidator_WhenTextIsMissing_ShouldBeInvalid(string? text)
        {
            var result = new CreateCommentDtoValidator().Validate(new CreateCommentDto { Text = text! });

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCommentDto.Text));
        }

        [Fact]
        public void CreateCommentDtoValidator_WhenTextExceedsMaximumLength_ShouldBeInvalid()
        {
            var result = new CreateCommentDtoValidator().Validate(
                new CreateCommentDto { Text = new string('a', CommentEntity.TextMaxLength + 1) });

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCommentDto.Text));
        }

        [Fact]
        public void CreateReplyCommandValidator_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new CreateReplyCommandValidator(new CreateCommentDtoValidator());
            var result = validator.Validate(new CreateReplyCommand(
                1,
                Guid.NewGuid(),
                new CreateCommentDto { Text = "Reply text" }));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void CreateReplyCommandValidator_WhenParentIdOrTextIsInvalid_ShouldIncludeErrors()
        {
            var validator = new CreateReplyCommandValidator(new CreateCommentDtoValidator());
            var result = validator.Validate(new CreateReplyCommand(
                0,
                Guid.NewGuid(),
                new CreateCommentDto { Text = " " }));

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateReplyCommand.ParentCommentId));
            Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(CreateCommentDto.Text)));
        }

        [Fact]
        public void CreateReplyCommandValidator_WhenReplyIsNull_ShouldBeInvalid()
        {
            var validator = new CreateReplyCommandValidator(new CreateCommentDtoValidator());
            var result = validator.Validate(new CreateReplyCommand(1, Guid.NewGuid(), null!));

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateReplyCommand.Reply));
        }
    }
}
