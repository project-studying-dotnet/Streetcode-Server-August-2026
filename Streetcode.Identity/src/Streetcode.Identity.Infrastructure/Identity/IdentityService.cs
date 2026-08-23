using FluentResults;
using Microsoft.AspNetCore.Identity;
using Streetcode.Identity.Application.Abstractions;

namespace Streetcode.Identity.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> CreateUserAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var applicationUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
        };

        var identityResult = await _userManager.CreateAsync(applicationUser, password);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors
                .Select(error => new Error(error.Description)
                    .WithMetadata("Code", error.Code));

            return Result.Fail<Guid>(errors);
        }

        return Result.Ok(applicationUser.Id);
    }
}
