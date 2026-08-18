using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Streetcode.WebApi.ExceptionHandlers;
using Xunit;
using FluentValidation;
using FluentValidation.Results;

namespace Streetcode.XUnitTest.ExceptionHandlerTests;

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

        var exception = new InvalidOperationException("Unexpected error.");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.False(handled);
        problemDetailsServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WhenValidationException_ShouldReturnBadRequest()
    {
        ProblemDetailsContext? capturedContext = null;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>();
        problemDetailsServiceMock
            .Setup(service => service.TryWriteAsync(
                It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(
                context => capturedContext = context)
            .Returns(new ValueTask<bool>(true));

        var handler = new ValidationExceptionHandler(problemDetailsServiceMock.Object);

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

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

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

        Assert.Equal(
            new[]
            {
                "Email is required.",
                "Email is invalid.",
            },
            problemDetails.Errors["Email"]);

        Assert.Equal(
            new[]
            {
                "Name is required.",
            },
            problemDetails.Errors["Name"]);
    }
}
