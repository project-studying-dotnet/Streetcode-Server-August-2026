using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Streetcode.WebApi.Controllers.Users;
using Streetcode.WebApi.Identity;
using Xunit;

namespace Streetcode.XUnitTest.Controllers;

public class RegistrationControllerTests
{
    [Fact]
    public async Task RegisterCreatesUserWithUserRole()
    {
        var manager = CreateUserManager();
        RegistrationUser? createdUser = null;
        manager.Setup(x => x.CreateAsync(It.IsAny<RegistrationUser>(), "Password123!"))
            .Callback<RegistrationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await new RegistrationController(manager.Object).Register(Request());

        Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdUser);
        Assert.Equal("member@example.com", createdUser.Email);
        Assert.Equal("Member", createdUser.Name);
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

        var result = await new RegistrationController(manager.Object).Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
        manager.Verify(x => x.CreateAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterReturnsIdentityErrorsWhenCreationFails()
    {
        var manager = CreateUserManager();
        manager.Setup(x => x.CreateAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" }));

        var result = await new RegistrationController(manager.Object).Register(Request());

        Assert.IsType<BadRequestObjectResult>(result);
        manager.Verify(x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterDeletesUserWhenRoleAssignmentFails()
    {
        var manager = CreateUserManager();
        manager.Setup(x => x.CreateAsync(It.IsAny<RegistrationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(x => x.AddToRoleAsync(It.IsAny<RegistrationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role unavailable" }));
        manager.Setup(x => x.DeleteAsync(It.IsAny<RegistrationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await new RegistrationController(manager.Object).Register(Request());

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
        manager.Verify(x => x.DeleteAsync(It.IsAny<RegistrationUser>()), Times.Once);
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
}
