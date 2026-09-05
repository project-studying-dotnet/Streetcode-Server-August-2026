// <copyright file="ExceptionHandlerMiddlewareTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.ExceptionHandlers
{
    using System.Net;
    using System.Text.Json;
    using FluentValidation;
    using FluentValidation.Results;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Streetcode.WebApi.ExceptionHandlers;
    using Xunit;

    public class ExceptionHandlerMiddlewareTests
    {
        [Fact]
        public async Task ExceptionHandler_WhenValidationException_ShouldReturnBadRequest()
        {
            await using WebApplication app = await CreateApplicationAsync();
            using HttpClient client = app.GetTestClient();

            using HttpResponseMessage response =
                await client.GetAsync("/validation");

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement problemDetails = document.RootElement;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(
                "Email is required.",
                problemDetails
                    .GetProperty("errors")
                    .GetProperty("Email")[0]
                    .GetString());
        }

        [Fact]
        public async Task ExceptionHandler_WhenUnhandledException_ShouldReturnInternalServerError()
        {
            await using WebApplication app = await CreateApplicationAsync();
            using HttpClient client = app.GetTestClient();

            using HttpResponseMessage response =
                await client.GetAsync("/unhandled");

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement problemDetails = document.RootElement;

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            Assert.False(
                string.IsNullOrWhiteSpace(
                    problemDetails
                        .GetProperty("traceId")
                        .GetString()));
            Assert.DoesNotContain("Sensitive message", body);
        }

        private static async Task<WebApplication> CreateApplicationAsync()
        {
            WebApplicationBuilder builder =
                WebApplication.CreateBuilder();

            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();

            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            WebApplication app = builder.Build();

            app.UseExceptionHandler();

            app.MapGet(
                "/validation",
                (HttpContext _) =>
                    Task.FromException(
                        new ValidationException(
                            [
                                new ValidationFailure(
                                    "Email",
                                    "Email is required."),
                            ])));

            app.MapGet(
                "/unhandled",
                (HttpContext _) =>
                    Task.FromException(
                        new InvalidOperationException(
                            "Sensitive message")));

            await app.StartAsync();

            return app;
        }
    }
}
