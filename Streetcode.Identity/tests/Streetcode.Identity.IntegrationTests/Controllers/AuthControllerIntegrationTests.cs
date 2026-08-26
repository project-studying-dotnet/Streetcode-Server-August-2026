using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Streetcode.Identity.Application.Features.Registration;
using Streetcode.Identity.IntegrationTests.Fixtures;
using Streetcode.Identity.WebApi.DTOs;
using Xunit;

namespace Streetcode.Identity.IntegrationTests.Controllers;

[Collection(MsSqlCollection.Name)]
public sealed class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WhenInputIsValid_ShouldReturnOk()
    {
        var request = new RegisterRequestDto
        {
            Email = $"httpuser-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword123!",
            PhoneNumber = "+380501112233"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();

        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.NotEqual(Guid.Empty, result.UserId);
    }

    [Fact]
    public async Task Register_WhenInputIsInvalid_ShouldReturnBadRequest()
    {
        var request = new RegisterRequestDto
        {
            Email = "invalid-email",
            Password = "123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturnBadRequestAndProblemDetails()
    {
        var request = new RegisterRequestDto
        {
            Email = $"duplicate-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword123!",
            PhoneNumber = "+380509998877"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Registration failed", problemDetails.Title);
    }
}