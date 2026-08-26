using FluentResults;
using Microsoft.AspNetCore.Identity;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.IntegrationEvents;
using Streetcode.Identity.Infrastructure.Persistence;

namespace Streetcode.Identity.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StreetcodeIdentityDbContext _dbContext;
    private readonly IOutboxWriter _outboxWriter;
    private readonly TimeProvider _timeProvider;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        StreetcodeIdentityDbContext dbContext,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _outboxWriter = outboxWriter;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> CreateUserAsync(string email, string password, DateTime? birthDate, string? phone, string? gender, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var applicationUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            Phone = phone,
            BirthDate = birthDate,
            Gender = gender,
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
}
