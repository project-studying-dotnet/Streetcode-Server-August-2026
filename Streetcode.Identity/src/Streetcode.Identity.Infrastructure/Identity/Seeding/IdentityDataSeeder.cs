using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Streetcode.Identity.Application.Common.Authorization;

namespace Streetcode.Identity.Infrastructure.Identity.Seeding;

public sealed class IdentityDataSeeder
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<IdentitySeedOptions> _options;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<IdentitySeedOptions> options,
        ILogger<IdentityDataSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var seedOptions = _options.Value;

        if (!seedOptions.Enabled)
        {
            _logger.LogInformation("Identity data seeding is disabled");
            return;
        }

        string[] roleNames =
        [
            RoleNames.User,
            RoleNames.Admin,
        ];

        foreach (var roleName in roleNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roleExists = await _roleManager.RoleExistsAsync(roleName);

            if (roleExists)
            {
                continue;
            }

            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName
            };

            var createRoleResult = await _roleManager.CreateAsync(role);

            if (!createRoleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createRoleResult.Errors.Select(error => $"{error.Code}: {error.Description}"));

                throw new InvalidOperationException(
                    $"Failed to create identity role '{roleName}': {errors}");
            }

            _logger.LogInformation(
                "Identity role {RoleName} was created",
                roleName);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var adminUser = await _userManager.FindByEmailAsync(seedOptions.AdminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = seedOptions.AdminEmail,
                UserName = seedOptions.AdminEmail
            };

            var createAdminResult = await _userManager.CreateAsync(
                adminUser,
                seedOptions.AdminPassword);

            if (!createAdminResult.Succeeded)
            {
                var errors = string.Join("; ",
                    createAdminResult.Errors.Select(
                        error => $"{error.Code}: {error.Description}"));

                throw new InvalidOperationException(
                    $"Failed to create the initial admin user: {errors}");
            }

            _logger.LogInformation(
                "Initial admin user {UserId} was created",
                adminUser.Id);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var isAdmin = await _userManager.IsInRoleAsync(
            adminUser,
            RoleNames.Admin);

        if (!isAdmin)
        {
            var addToRoleResult = await _userManager.AddToRoleAsync(
                adminUser,
                RoleNames.Admin);

            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    addToRoleResult.Errors.Select(
                        error => $"{error.Code}: {error.Description}"));

                throw new InvalidOperationException(
                    $"Failed to assign the Admin role: {errors}");
            }

            _logger.LogInformation(
                "User {UserId} was assigned to role {RoleName}",
                adminUser.Id,
                RoleNames.Admin);
        }
    }
}
