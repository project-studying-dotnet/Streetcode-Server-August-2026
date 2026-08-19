// <copyright file="ValidationExceptionHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ExceptionHandlerTests
{
    using FluentValidation;
    using FluentValidation.Results;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using Streetcode.WebApi.ExceptionHandlers;
    using Xunit;

    public class ValidationExceptionHandlerTests
    {
        [Fact]
        public async Task TryHandleAsync_WhenExceptionIsNotValidationException_ShouldReturnFalse()
        {
            var problemDetailsServiceMock =
                new Mock<IProblemDetailsService>();

            var handler = new ValidationExceptionHandler(
                problemDetailsServiceMock.Object);

            var httpContext = new DefaultHttpContext();

            var exception =
                new InvalidOperationException("Unexpected error.");

            bool handled = await handler.TryHandleAsync(
                httpContext,
                exception,
                CancellationToken.None);

            Assert.False(handled);
            problemDetailsServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryHandleAsync_WhenValidationException_ShouldReturnBadRequest()
        {
            ProblemDetailsContext? capturedContext = null;

            var problemDetailsServiceMock =
                new Mock<IProblemDetailsService>();

            problemDetailsServiceMock
                .Setup(service => service.TryWriteAsync(
                    It.IsAny<ProblemDetailsContext>()))
                .Callback<ProblemDetailsContext>(
                    context => capturedContext = context)
                .Returns(new ValueTask<bool>(true));

            var handler = new ValidationExceptionHandler(
                problemDetailsServiceMock.Object);

            var httpContext = new DefaultHttpContext();

            var failures = new[]
            {
                new ValidationFailure(
                    "Email",
                    "Email is required."),
                new ValidationFailure(
                    "Email",
                    "Email is invalid."),
                new ValidationFailure(
                    "Name",
                    "Name is required."),
            };

            var exception = new ValidationException(failures);

            bool handled = await handler.TryHandleAsync(
                httpContext,
                exception,
                CancellationToken.None);

            Assert.True(handled);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                httpContext.Response.StatusCode);

            var problemDetails =
                Assert.IsType<ValidationProblemDetails>(
                    capturedContext?.ProblemDetails);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                problemDetails.Status);

            Assert.Collection(
                problemDetails.Errors["Email"],
                error => Assert.Equal(
                    "Email is required.",
                    error),
                error => Assert.Equal(
                    "Email is invalid.",
                    error));

            Assert.Collection(
                problemDetails.Errors["Name"],
                error => Assert.Equal(
                    "Name is required.",
                    error));
        }
    }
}