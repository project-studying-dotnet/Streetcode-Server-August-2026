using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Abstractions.Security;
using Streetcode.Identity.Application.Features.Authentication.Refresh;
using Streetcode.Identity.Application.Features.Registration;
using Streetcode.Identity.IntegrationTests.Fixtures;
using Streetcode.Identity.WebApi.DTOs;
using Xunit;

namespace Streetcode.Identity.IntegrationTests.Controllers;

[Collection(MsSqlCollection.Name)]
public sealed class AuthControllerIntegrationTests
{
    private readonly MsSqlContainerFixture _fixture;

    public AuthControllerIntegrationTests(MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private IdentityWebApplicationFactory CreateFactory()
    {
        return new IdentityWebApplicationFactory(_fixture.ConnectionString);
    }

    [Fact]
    public async Task Register_WhenInputIsValid_ShouldReturnOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new RegisterRequestDto
        {
            Email = $"httpuser-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword123!",
            PhoneNumber = "+380501112233"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();

        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.NotEqual(Guid.Empty, result.UserId);
    }

    [Fact]
    public async Task Register_WhenInputIsInvalid_ShouldReturnBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new RegisterRequestDto
        {
            Email = "invalid-email",
            Password = "123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturnBadRequestAndProblemDetails()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new RegisterRequestDto
        {
            Email = $"duplicate-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword123!",
            PhoneNumber = "+380509998877"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Registration failed", problemDetails.Title);
    }

    [Fact]
    public async Task Refresh_WhenRefreshTokenIsEmpty_ShouldReturnBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new RefreshSessionRequestDto
        {
            RefreshToken = string.Empty,
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(
            "One or more validation errors occurred.",
            problemDetails.Title);
        Assert.Contains(
            nameof(RefreshSessionRequestDto.RefreshToken),
            problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task Refresh_WhenRefreshTokenDoesNotExist_ShouldReturnUnauthorized()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new RefreshSessionRequestDto
        {
            RefreshToken = $"unknown-{Guid.NewGuid():N}"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(
            "Refresh session failed",
            problemDetails.Title);
        Assert.Equal(
            "The refresh token is invalid or inactive",
            problemDetails.Detail);
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problemDetails.Status);
    }

    [Fact]
    public async Task Refresh_WhenRefreshTokenIsValid_ShouldReturnOkAndRotateToken()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var registerRequest = new RegisterRequestDto
        {
            Email = $"refresh-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword123!",
            PhoneNumber = "+380501234567"
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            registerRequest);

        registerResponse.EnsureSuccessStatusCode();

        var registeredUser =
            await registerResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();

        Assert.NotNull(registeredUser);

        await using var scope = factory.Services.CreateAsyncScope();
        var refreshTokenService = scope.ServiceProvider
            .GetRequiredService<IRefreshTokenService>();

        var issueResult = await refreshTokenService.IssueAsync(
            registeredUser.UserId,
            CancellationToken.None);

        Assert.True(issueResult.IsSuccess);

        var originalRefreshToken = issueResult.Value.Token;
        var request = new RefreshSessionRequestDto
        {
            RefreshToken = originalRefreshToken
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<RefreshSessionResponse>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotEqual(originalRefreshToken, result.RefreshToken);
        Assert.True(result.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
        Assert.True(result.RefreshTokenExpiresAt > DateTimeOffset.UtcNow);
    }
}
