using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.WebApi.ExceptionHandling;

namespace Streetcode.Identity.IntegrationTests.WebApi;

public sealed class ValidationExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsNotValidationException_ShouldReturnFalse()
    {
        var services = new ServiceCollection();

        services.AddOptions();
        services.AddProblemDetails();

        await using var serviceProvider =
            services.BuildServiceProvider();

        var problemDetailsService =
            serviceProvider.GetRequiredService<IProblemDetailsService>();

        var handler =
            new ValidationExceptionHandler(problemDetailsService);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };

        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("Unexpected failure"),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(
            StatusCodes.Status200OK,
            httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_WhenValidationExceptionOccurs_ShouldWriteBadRequestProblemDetails()
    {
        var services = new ServiceCollection();

        services.AddOptions();
        services.AddProblemDetails();

        await using var serviceProvider =
            services.BuildServiceProvider();

        var problemDetailsService =
            serviceProvider.GetRequiredService<IProblemDetailsService>();

        var handler = new ValidationExceptionHandler(problemDetailsService);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };
        httpContext.Request.Path = "/api/registration";
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Headers.Accept = "application/problem+json";

        var emailFailure = new ValidationFailure(
            "Email",
            "Email is required.")
        {
            ErrorCode = "Email.Required",
        };

        var validationException = new ValidationException([emailFailure]);

        var handled = await handler.TryHandleAsync(
            httpContext,
            validationException,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;

        using var document =
            await JsonDocument.ParseAsync(httpContext.Response.Body);

        var root = document.RootElement;

        Assert.Equal(
            "One or more validation errors occurred.",
            root.GetProperty("title").GetString());

        var errors = root.GetProperty("errors");

        Assert.Equal(
            "Email is required.",
            errors.GetProperty("Email")[0].GetString());

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            "/api/registration",
            root.GetProperty("instance").GetString());

        var errorCodes = root.GetProperty("errorCodes");

        Assert.Equal(
            "Email.Required",
            errorCodes.GetProperty("Email")[0].GetString());
    }
}
