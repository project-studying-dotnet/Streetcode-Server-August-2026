using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Application.Common.Authorization;
using Streetcode.Identity.Application.Features.Registration;
using Streetcode.Identity.Infrastructure.Persistence;
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
    public async Task Register_WhenAdminSeedingIsDisabledAndRolesDoNotExist_ShouldCreateRolesAndRegisterUser()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(
            _fixture.ConnectionString)
        {
            InitialCatalog = $"IdentityRegistration-{Guid.NewGuid():N}",
        };

        var dbContextOptions =
            new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
                .UseSqlServer(connectionStringBuilder.ConnectionString)
                .Options;

        await using var dbContext =
            new StreetcodeIdentityDbContext(dbContextOptions);

        await dbContext.Database.MigrateAsync();

        Assert.False(await dbContext.Roles.AnyAsync());

        await using var factory = new IdentityWebApplicationFactory(
            connectionStringBuilder.ConnectionString);

        using var client = factory.CreateClient();

        var request = new RegisterRequestDto
        {
            Email = $"disabled-seed-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword123!",
            PhoneNumber = "+380501112233"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.EnsureSuccessStatusCode();

        dbContext.ChangeTracker.Clear();

        Assert.True(await dbContext.Roles.AnyAsync(role =>
            role.Name == RoleNames.User));

        Assert.True(await dbContext.Roles.AnyAsync(role =>
            role.Name == RoleNames.Admin));

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(applicationUser =>
                applicationUser.Email == request.Email);

        var assignedRole = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles
                on userRole.RoleId equals role.Id
            where userRole.UserId == user.Id
            select role.Name)
            .SingleAsync();

        Assert.Equal(RoleNames.User, assignedRole);
        Assert.Equal(1, await dbContext.Users.CountAsync());
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
}
