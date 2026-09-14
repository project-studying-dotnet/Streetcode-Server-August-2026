// <copyright file="GlobalExceptionHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ExceptionHandlers
{
    using System.Text.Json;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Streetcode.WebApi.ExceptionHandlers;
    using Xunit;

    public class GlobalExceptionHandlerTests
    {
        [Fact]
        public async Task TryHandleAsync_WhenExceptionOccurs_WritesProblemDetails()
        {
            var problemDetailsServiceMock = new Mock<IProblemDetailsService>();
            ProblemDetailsContext? capturedContext = null;
            problemDetailsServiceMock
                .Setup(service =>
                    service.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
                .Callback<ProblemDetailsContext>(context =>
                    capturedContext = context)
                .ReturnsAsync(true);
            var handler = new GlobalExceptionHandler(
                NullLogger<GlobalExceptionHandler>.Instance,
                problemDetailsServiceMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/test";
            httpContext.TraceIdentifier = "test-trace-id";
            var exception = new InvalidOperationException("Sensitive message");

            var result = await handler.TryHandleAsync(
                httpContext,
                exception,
                CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                StatusCodes.Status500InternalServerError,
                httpContext.Response.StatusCode);
            Assert.NotNull(capturedContext);
            Assert.Same(httpContext, capturedContext.HttpContext);
            Assert.Same(exception, capturedContext.Exception);
            var problemDetails = capturedContext.ProblemDetails;
            Assert.Equal(
                StatusCodes.Status500InternalServerError,
                problemDetails.Status);
            Assert.Equal("Internal Server Error", problemDetails.Title);
            Assert.Equal("An unexpected error occurred.", problemDetails.Detail);
            Assert.Equal("/api/test", problemDetails.Instance);
            Assert.Equal(
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1",
                problemDetails.Type);
            var traceId = Assert.IsType<string>(
                problemDetails.Extensions["traceId"]);
            Assert.Equal("test-trace-id", traceId);
            problemDetailsServiceMock.Verify(
                service => service.TryWriteAsync(It.IsAny<ProblemDetailsContext>()),
                Times.Once);
        }

        [Fact]
        public async Task TryHandleAsync_WhenProblemDetailsWriterReturnsFalse_ShouldWriteFallbackResponse()
        {
            var problemDetailsServiceMock = new Mock<IProblemDetailsService>();

            problemDetailsServiceMock
                .Setup(service =>
                    service.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
                .ReturnsAsync(false);

            var handler = new GlobalExceptionHandler(
                NullLogger<GlobalExceptionHandler>.Instance,
                problemDetailsServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/test";
            httpContext.TraceIdentifier = "fallback-trace-id";
            httpContext.Response.Body = new MemoryStream();

            var exception =
                new InvalidOperationException("Sensitive message");

            bool handled = await handler.TryHandleAsync(
                httpContext,
                exception,
                CancellationToken.None);

            httpContext.Response.Body.Position = 0;

            using JsonDocument document = await JsonDocument.ParseAsync(
                httpContext.Response.Body);

            JsonElement response = document.RootElement;

            Assert.True(handled);
            Assert.Equal(
                StatusCodes.Status500InternalServerError,
                httpContext.Response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                httpContext.Response.ContentType);
            Assert.Equal(
                "fallback-trace-id",
                response.GetProperty("traceId").GetString());
            Assert.Equal(
                "An unexpected error occurred.",
                response.GetProperty("detail").GetString());
            Assert.Equal(
                "/api/test",
                response.GetProperty("instance").GetString());
            Assert.DoesNotContain(
                "Sensitive message",
                response.GetRawText());
        }
    }
}
