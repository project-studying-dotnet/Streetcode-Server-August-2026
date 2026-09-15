// <copyright file="UpdateCommentValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Streetcode.Comments;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Update;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Validators;
    using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;
    using Xunit;

    public class UpdateCommentValidatorsTests
    {
        [Fact]
        public void ValidateDto_WhenTextIsValid_ShouldBeValid()
        {
            var result = new UpdateCommentDtoValidator().Validate(
                new UpdateCommentDto { Text = new string('a', CommentEntity.TextMaxLength) });

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateDto_WhenTextIsMissing_ShouldBeInvalid(string? text)
        {
            var result = new UpdateCommentDtoValidator().Validate(new UpdateCommentDto { Text = text! });

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentDto.Text));
        }

        [Fact]
        public void ValidateDto_WhenTextExceedsMaximumLength_ShouldBeInvalid()
        {
            var result = new UpdateCommentDtoValidator().Validate(
                new UpdateCommentDto { Text = new string('a', CommentEntity.TextMaxLength + 1) });

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentDto.Text));
        }

        [Fact]
        public void ValidateCommand_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new UpdateCommentCommandValidator(new UpdateCommentDtoValidator());
            var result = validator.Validate(new UpdateCommentCommand(1, new UpdateCommentDto { Text = "Updated comment" }));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateCommand_WhenIdOrCommentIsInvalid_ShouldIncludeErrors()
        {
            var validator = new UpdateCommentCommandValidator(new UpdateCommentDtoValidator());
            var result = validator.Validate(new UpdateCommentCommand(0, new UpdateCommentDto { Text = " " }));

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentCommand.Id));
            Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(UpdateCommentDto.Text)));
        }

        [Fact]
        public void ValidateCommand_WhenCommentIsNull_ShouldBeInvalid()
        {
            var validator = new UpdateCommentCommandValidator(new UpdateCommentDtoValidator());
            var result = validator.Validate(new UpdateCommentCommand(1, null!));

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentCommand.Comment));
        }
    }
}
