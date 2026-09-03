using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Common.Authorization;
using Streetcode.Identity.Infrastructure;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Identity.Seeding;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Identity.Seeding;

[Collection(MsSqlCollection.Name)]
public sealed class IdentityDataSeederIntegrationTests
{
    private readonly MsSqlContainerFixture _fixture;

    public IdentityDataSeederIntegrationTests(
        MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeedAsync_WhenEnabled_ShouldCreateRolesAndAdminUser()
    {
        var adminEmail = $"seed-admin-{Guid.NewGuid():N}@example.com";
        const string adminPassword = "ValidAdminPassword123!";

        await using var serviceProvider =
            CreateServiceProvider(adminEmail, adminPassword);

        await using var scope =
            serviceProvider.CreateAsyncScope();

        var seeder = scope.ServiceProvider
            .GetRequiredService<IdentityDataSeeder>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        await seeder.SeedAsync();

        Assert.True(
            await roleManager.RoleExistsAsync(RoleNames.User));

        Assert.True(
            await roleManager.RoleExistsAsync(RoleNames.Admin));

        var admin = await userManager.FindByEmailAsync(adminEmail);

        Assert.NotNull(admin);

        Assert.True(
            await userManager.IsInRoleAsync(admin, RoleNames.Admin));

        Assert.NotNull(admin.PasswordHash);
        Assert.NotEqual(adminPassword, admin.PasswordHash);
    }

    [Fact]
    public async Task SeedAsync_WhenCalledTwice_ShouldNotCreateDuplicates()
    {
        var adminEmail = $"seed-admin-{Guid.NewGuid():N}@example.com";
        const string adminPassword = "ValidAdminPassword123!";

        await using var serviceProvider =
            CreateServiceProvider(adminEmail, adminPassword);

        await using var scope =
            serviceProvider.CreateAsyncScope();

        var seeder = scope.ServiceProvider
            .GetRequiredService<IdentityDataSeeder>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        await seeder.SeedAsync();

        var adminAfterFirstRun =
            await userManager.FindByEmailAsync(adminEmail);

        await seeder.SeedAsync();

        var adminAfterSecondRun =
            await userManager.FindByEmailAsync(adminEmail);

        Assert.NotNull(adminAfterFirstRun);
        Assert.NotNull(adminAfterSecondRun);
        Assert.Equal(adminAfterFirstRun.Id, adminAfterSecondRun.Id);

        var normalizedEmail = userManager.NormalizeEmail(adminEmail);

        var adminUsersCount = await userManager.Users.CountAsync(
            user => user.NormalizedEmail == normalizedEmail);

        Assert.Equal(1, adminUsersCount);

        var normalizedAdminRole = roleManager.NormalizeKey(RoleNames.Admin);
        var normalizedUserRole = roleManager.NormalizeKey(RoleNames.User);

        var adminRolesCount = await roleManager.Roles.CountAsync(
            role => role.NormalizedName == normalizedAdminRole);

        var userRolesCount = await roleManager.Roles.CountAsync(
            role => role.NormalizedName == normalizedUserRole);

        Assert.Equal(1, adminRolesCount);
        Assert.Equal(1, userRolesCount);
    }

    [Fact]
    public async Task SeedAsync_WhenDisabled_ShouldNotChangeIdentityData()
    {
        var adminEmail = $"disabled-seed-{Guid.NewGuid():N}@example.com";
        const string adminPassword = "ValidAdminPassword123!";

        await using var serviceProvider = CreateServiceProvider(
            adminEmail,
            adminPassword,
            enabled: false);

        await using var scope = serviceProvider.CreateAsyncScope();

        var seeder = scope.ServiceProvider
            .GetRequiredService<IdentityDataSeeder>();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<StreetcodeIdentityDbContext>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var usersBefore = await dbContext.Users.CountAsync();
        var rolesBefore = await dbContext.Roles.CountAsync();

        await seeder.SeedAsync();

        var usersAfter = await dbContext.Users.CountAsync();
        var rolesAfter = await dbContext.Roles.CountAsync();
        var admin = await userManager.FindByEmailAsync(adminEmail);

        Assert.Equal(usersBefore, usersAfter);
        Assert.Equal(rolesBefore, rolesAfter);
        Assert.Null(admin);
    }

    private ServiceProvider CreateServiceProvider(
        string adminEmail,
        string adminPassword,
        bool enabled = true)
    {
        var configurationValues =
            new Dictionary<string, string?>
            {
                [$"{IdentitySeedOptions.SectionName}:Enabled"] =
                    enabled.ToString(),
                [$"{IdentitySeedOptions.SectionName}:AdminEmail"] =
                    adminEmail,
                [$"{IdentitySeedOptions.SectionName}:AdminPassword"] =
                    adminPassword,
            };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddInfrastructure(_fixture.ConnectionString);
        services.AddIdentitySeeding(configuration);

        return services.BuildServiceProvider(validateScopes: true);
    }
}
