using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.Common.Authorization;
using Streetcode.Identity.Application.Features.Authentication.Google;
using Streetcode.Identity.Application.IntegrationEvents;
using Streetcode.Identity.Infrastructure.Persistence;

namespace Streetcode.Identity.Infrastructure.Identity;

public sealed class GoogleIdentityService(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    StreetcodeIdentityDbContext db,
    IOutboxWriter outbox,
    TimeProvider clock) : IGoogleIdentityService
{
    private const string Provider = "Google";

    public async Task<Result<UserTokenData>> AuthenticateAsync(GoogleAccount account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByLoginAsync(Provider, account.Subject);
        if (user is not null) return await TokenDataAsync(user, cancellationToken);

        // Email equality is not proof that the external identity owns the local account.
        if (await users.FindByEmailAsync(account.Email) is not null)
            return Result.Fail<UserTokenData>(GoogleAuthErrors.LinkRequired());

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            user = new ApplicationUser { Id = Guid.NewGuid(), Email = account.Email, UserName = account.Email };
            var created = await users.CreateAsync(user); // External-only account: no invented password.
            if (!created.Succeeded)
                return Result.Fail<UserTokenData>(GoogleAuthErrors.Conflict());
            var role = await users.AddToRoleAsync(user, RoleNames.User);
            if (!role.Succeeded)
                return Result.Fail<UserTokenData>(GoogleAuthErrors.Unavailable());
            var login = await users.AddLoginAsync(user, new UserLoginInfo(Provider, account.Subject, Provider));
            if (!login.Succeeded)
                return Result.Fail<UserTokenData>(GoogleAuthErrors.Conflict());

            await outbox.AddAsync(new UserAccessChangedV1(Guid.NewGuid(), user.Id, user.IsActive,
                user.AccessVersion, clock.GetUtcNow()), user.Id.ToString("D"), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // A simultaneous registration/link must never silently merge accounts.
            return Result.Fail<UserTokenData>(GoogleAuthErrors.Conflict());
        }

        return await TokenDataAsync(user, cancellationToken);
    }

    public async Task<Result> LinkAsync(GoogleAccount account, Guid userId, long accessVersion,
        string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId.ToString("D"));
        if (user is null || !user.IsActive || user.AccessVersion != accessVersion)
            return Result.Fail(GoogleAuthErrors.Unauthorized());

        // Reauthentication protects linking even if an old access token is still available.
        var passwordCheck = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!passwordCheck.Succeeded) return Result.Fail(GoogleAuthErrors.Unauthorized());

        if (users.NormalizeEmail(user.Email!) != users.NormalizeEmail(account.Email))
            return Result.Fail(GoogleAuthErrors.Conflict());

        var owner = await users.FindByLoginAsync(Provider, account.Subject);
        if (owner is not null)
            return owner.Id == user.Id ? Result.Ok() : Result.Fail(GoogleAuthErrors.Conflict());

        // This first version allows one Google identity per local account.
        if ((await users.GetLoginsAsync(user)).Any(login => login.LoginProvider == Provider))
            return Result.Fail(GoogleAuthErrors.Conflict());

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var linked = await users.AddLoginAsync(user, new UserLoginInfo(Provider, account.Subject, Provider));
            return linked.Succeeded ? Result.Ok() : Result.Fail(GoogleAuthErrors.Conflict());
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Fail(GoogleAuthErrors.Conflict());
        }
    }

    private async Task<Result<UserTokenData>> TokenDataAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!user.IsActive || string.IsNullOrWhiteSpace(user.Email) || await users.IsLockedOutAsync(user))
            return Result.Fail<UserTokenData>(GoogleAuthErrors.Unauthorized());
        var roles = await users.GetRolesAsync(user);
        return Result.Ok(new UserTokenData(user.Id, user.Email, roles.ToArray(), user.AccessVersion, user.IsActive));
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
