// <copyright file="UpdateCommentValidatorsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.ValidatorTests
{
    using Streetcode.BLL.DTO.Streetcode.Comments;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Update;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Validators;
    using Xunit;
    using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

    public class UpdateCommentValidatorsTests
    {
        [Fact]
        public void ValidateDto_WhenTextIsValid_ShouldBeValid()
        {
            var result = new UpdateCommentDtoValidator().Validate(
                new UpdateCommentDto { Text = new string('a', CommentEntity.TextMaxLength), RowVersion = new byte[] { 1 } });

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateDto_WhenTextIsMissing_ShouldBeInvalid(string? text)
        {
            var result = new UpdateCommentDtoValidator().Validate(new UpdateCommentDto { Text = text!, RowVersion = new byte[] { 1 } });

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentDto.Text));
        }

        [Fact]
        public void ValidateDto_WhenTextExceedsMaximumLength_ShouldBeInvalid()
        {
            var result = new UpdateCommentDtoValidator().Validate(
                new UpdateCommentDto { Text = new string('a', CommentEntity.TextMaxLength + 1), RowVersion = new byte[] { 1 } });

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentDto.Text));
        }

        [Fact]
        public void ValidateCommand_WhenCommandIsValid_ShouldBeValid()
        {
            var validator = new UpdateCommentCommandValidator(new UpdateCommentDtoValidator());
            var result = validator.Validate(new UpdateCommentCommand(1, Guid.NewGuid(), new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } }));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateCommand_WhenIdOrCommentIsInvalid_ShouldIncludeErrors()
        {
            var validator = new UpdateCommentCommandValidator(new UpdateCommentDtoValidator());
            var result = validator.Validate(new UpdateCommentCommand(0, Guid.NewGuid(), new UpdateCommentDto { Text = " ", RowVersion = new byte[] { 1 } }));

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentCommand.Id));
            Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(UpdateCommentDto.Text)));
        }

        [Fact]
        public void ValidateCommand_WhenCommentIsNull_ShouldBeInvalid()
        {
            var validator = new UpdateCommentCommandValidator(new UpdateCommentDtoValidator());
            var result = validator.Validate(new UpdateCommentCommand(1, Guid.NewGuid(), null!));

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentCommand.Comment));
        }

        [Fact]
        public void ValidateDto_WhenRowVersionIsMissing_ShouldBeInvalid()
        {
            var result = new UpdateCommentDtoValidator().Validate(
                new UpdateCommentDto { Text = "Updated comment", RowVersion = Array.Empty<byte>() });

            Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentDto.RowVersion));
        }
    }
}
