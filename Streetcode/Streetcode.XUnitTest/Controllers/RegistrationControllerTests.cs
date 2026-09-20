using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Streetcode.WebApi.Controllers.Users;
using Streetcode.WebApi.Extensions;
using Streetcode.WebApi.Identity;
using Xunit;

namespace Streetcode.XUnitTest.Controllers;

public class RegistrationControllerTests
{
    [Fact]
    public async Task RegisterCreatesUserWithUserRole()
    {
        var manager = CreateUserManager();
        var transaction = CreateTransactionFactory();
        RegistrationUser? createdUser = null;
        manager.Setup(x => x.CreateAsync(It.IsAny<RegistrationUser>(), "Password123!"))
            .Callback<RegistrationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateController(manager, transaction).Register(Request());

        Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdUser);
        Assert.Equal("member@example.com", createdUser.Email);
        Assert.Equal("Member", createdUser.Name);
        transaction.Transaction.Verify(x => x.CommitAsync(default), Times.Once);
        manager.Verify(x => x.AddToRoleAsync(createdUser, "User"), Times.Once);
    }

    [Fact]
    public async Task RegisterRejectsBlankNameWithoutCreatingUser()
    {
        var manager = CreateUserManager();
        var request = Request();
        request = new RegisterRequest
        {
            Name = " ",
            Surname = request.Surname,
            Email = request.Email,
            Password = request.Password,
        };

        var result = await CreateController(manager).Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
        manager.Verify(x => x.CreateAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterReturnsIdentityErrorsWhenCreationFails()
    {
        var manager = CreateUserManager();
        manager.Setup(x => x.CreateAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" }));

        var result = await CreateController(manager).Register(Request());

        Assert.IsType<BadRequestObjectResult>(result);
        manager.Verify(x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterRollsBackWhenRoleAssignmentFails()
    {
        var manager = CreateUserManager();
        var transaction = CreateTransactionFactory();
        manager.Setup(x => x.CreateAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role unavailable" }));
        var result = await CreateController(manager, transaction).Register(Request());

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
        transaction.Transaction.Verify(x => x.CommitAsync(default), Times.Never);
        manager.Verify(x => x.DeleteAsync(It.IsAny<RegistrationUser>()), Times.Never);
    }

    [Fact]
    public async Task RegisterRollsBackWhenRoleAssignmentThrows()
    {
        var manager = CreateUserManager();
        var transaction = CreateTransactionFactory();
        manager.Setup(x => x.CreateAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), "User"))
            .ThrowsAsync(new InvalidOperationException("Role is missing"));

        var result = await CreateController(manager, transaction).Register(Request());

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
        transaction.Transaction.Verify(x => x.CommitAsync(default), Times.Never);
        manager.Verify(x => x.DeleteAsync(It.IsAny<RegistrationUser>()), Times.Never);
    }

    [Fact]
    public async Task SeedIdentityRejectsExistingNonAdministratorAccount()
    {
        var user = new RegistrationUser { Email = "admin@example.com" };
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync("admin@example.com")).ReturnsAsync(user);
        userManager.Setup(x => x.IsInRoleAsync(user, "MainAdministrator")).ReturnsAsync(false);
        var roleManager = CreateRoleManager();
        roleManager.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        using var services = new ServiceCollection()
            .AddSingleton(userManager.Object)
            .AddSingleton(roleManager.Object)
            .BuildServiceProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Admin:Email"] = "admin@example.com",
                ["Identity:Admin:Password"] = "Password123!",
            })
            .Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WebApplicationExtensions.SeedIdentityAsync(services, configuration));

        Assert.Contains("non-administrator", exception.Message);
        userManager.Verify(
            x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), "MainAdministrator"),
            Times.Never);
    }

    private static RegisterRequest Request() => new()
    {
        Name = " Member ",
        Surname = "Example",
        Email = " member@example.com ",
        Password = "Password123!",
    };

    private static Mock<UserManager<RegistrationUser>> CreateUserManager() => new(
        Mock.Of<IUserStore<RegistrationUser>>(),
        Options.Create(new IdentityOptions()),
        Mock.Of<IPasswordHasher<RegistrationUser>>(),
        Array.Empty<IUserValidator<RegistrationUser>>(),
        Array.Empty<IPasswordValidator<RegistrationUser>>(),
        Mock.Of<ILookupNormalizer>(),
        new IdentityErrorDescriber(),
        Mock.Of<IServiceProvider>(),
        Mock.Of<ILogger<UserManager<RegistrationUser>>>());

    private static RegistrationController CreateController(
        Mock<UserManager<RegistrationUser>> manager,
        TransactionFactoryMock? transaction = null) =>
        new(manager.Object, (transaction ?? CreateTransactionFactory()).Factory.Object);

    private static TransactionFactoryMock CreateTransactionFactory()
    {
        var transaction = new Mock<IDbContextTransaction>();
        transaction.Setup(x => x.CommitAsync(default)).Returns(Task.CompletedTask);
        transaction.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var factory = new Mock<IRegistrationTransactionFactory>();
        factory.Setup(x => x.BeginAsync()).ReturnsAsync(transaction.Object);
        return new TransactionFactoryMock(factory, transaction);
    }

    private static Mock<RoleManager<IdentityRole>> CreateRoleManager() => new(
        Mock.Of<IRoleStore<IdentityRole>>(),
        Array.Empty<IRoleValidator<IdentityRole>>(),
        Mock.Of<ILookupNormalizer>(),
        new IdentityErrorDescriber(),
        Mock.Of<ILogger<RoleManager<IdentityRole>>>());

    private sealed record TransactionFactoryMock(
        Mock<IRegistrationTransactionFactory> Factory,
        Mock<IDbContextTransaction> Transaction);
}
