using Microsoft.EntityFrameworkCore;
using Streetcode.DAL.Persistence;
using Streetcode.WebApi.Identity;
using Microsoft.AspNetCore.Identity;

namespace Streetcode.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrations(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        await using var scope = app.Services.CreateAsyncScope();
        try
        {
            var streetcodeContext = scope.ServiceProvider.GetRequiredService<StreetcodeDbContext>();
            await streetcodeContext.Database.MigrateAsync();
            var registrationContext = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();
            await registrationContext.Database.MigrateAsync();
            await SeedIdentityAsync(scope.ServiceProvider, app.Configuration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured during startup migration");
            throw;
        }
    }

    public static async Task SeedIdentityAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "User", "Moderator", "Admin", "MainAdministrator" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not seed role {role}.");
                }
            }
        }

        var email = configuration["Identity:Admin:Email"];
        var password = configuration["Identity:Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<RegistrationUser>>();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is not null)
        {
            if (!await userManager.IsInRoleAsync(admin, "MainAdministrator"))
            {
                throw new InvalidOperationException(
                    "The configured administrator email belongs to a non-administrator account.");
            }

            if (!await userManager.CheckPasswordAsync(admin, password))
            {
                throw new InvalidOperationException(
                    "The configured administrator password does not match the existing account.");
            }

            return;
        }

        admin = new RegistrationUser
        {
            UserName = email,
            Email = email,
            Name = "Main",
            Surname = "Administrator",
        };
        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException("Could not seed administrator: " +
                string.Join(", ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(admin, "MainAdministrator");
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException("Could not assign administrator role.");
        }
    }
}
