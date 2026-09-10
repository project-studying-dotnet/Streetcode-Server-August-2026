using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.IntegrationEvents;
using Streetcode.Identity.Infrastructure.Persistence;

namespace Streetcode.Identity.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StreetcodeIdentityDbContext _dbContext;
    private readonly IOutboxWriter _outboxWriter;
    private readonly DummyPasswordHash _dummyPasswordHash;
    private readonly TimeProvider _timeProvider;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        StreetcodeIdentityDbContext dbContext,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider,
        DummyPasswordHash dummyPasswordHash)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _outboxWriter = outboxWriter;
        _timeProvider = timeProvider;
        _dummyPasswordHash = dummyPasswordHash;
    }

    public async Task<Result<Guid>> CreateUserAsync(string email, string password, string? phoneNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var applicationUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            PhoneNumber = phoneNumber
        };

        var identityResult = await _userManager.CreateAsync(applicationUser, password);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors
                .Select(error => new Error(error.Description)
                    .WithMetadata("Code", error.Code));

            return Result.Fail<Guid>(errors);
        }

        var integrationEvent = new UserAccessChangedV1(
            Guid.NewGuid(),
            applicationUser.Id,
            applicationUser.IsActive,
            applicationUser.AccessVersion,
            _timeProvider.GetUtcNow());

        await _outboxWriter.AddAsync(
            integrationEvent,
            applicationUser.Id.ToString("D"),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Ok(applicationUser.Id);
    }

    public async Task<Result<UserTokenData>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return CreateInvalidCredentialsFailure();
        }

        var user = await _userManager.FindByEmailAsync(email);

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null)
        {
            _ = _userManager.PasswordHasher.VerifyHashedPassword(
                new ApplicationUser(),
                _dummyPasswordHash.Value,
                password);

            return CreateInvalidCredentialsFailure();
        }

        var signInResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                password,
                lockoutOnFailure: true);

        cancellationToken.ThrowIfCancellationRequested();

        if (!signInResult.Succeeded)
        {
            return CreateInvalidCredentialsFailure();
        }

        if (!user.IsActive ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return CreateInvalidCredentialsFailure();
        }

        var roles = await _userManager.GetRolesAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        return Result.Ok(new UserTokenData(
            user.Id,
            user.Email,
            roles.ToArray(),
            user.AccessVersion,
            user.IsActive));
    }

    public async Task<Result<UserTokenData>> GetUserTokenDataAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == Guid.Empty)
        {
            return CreateUserTokenDataFailure();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == userId,
                cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return CreateUserTokenDataFailure();
        }

        var roles = await _userManager.GetRolesAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        var userTokenData = new UserTokenData(
            user.Id,
            user.Email,
            roles.ToArray(),
            user.AccessVersion,
            user.IsActive);

        return Result.Ok(userTokenData);
    }

    private static Result<UserTokenData> CreateInvalidCredentialsFailure()
    {
        return Result.Fail<UserTokenData>(
            new Error("Invalid email or password")
                .WithMetadata("Code", "Identity.InvalidCredentials"));
    }

    private static Result<UserTokenData> CreateUserTokenDataFailure()
    {
        return Result.Fail<UserTokenData>(
            new Error("The user could not be loaded")
                .WithMetadata("Code", "Identity.UserNotFound"));
    }
}
