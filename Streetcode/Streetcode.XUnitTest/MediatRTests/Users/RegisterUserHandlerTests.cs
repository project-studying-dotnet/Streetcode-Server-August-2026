// <copyright file="RegisterUserHandlerTests.cs" company="Streetcode">
// Copyright (c) Streetcode. All rights reserved.
// </copyright>

// Додав header щоб sonarqube не сварився + юзінги в неймспейс кинув, щоб sonarqube не сварився

namespace Streetcode.XUnitTest.MediatRTests.Users.Register
{

    using AutoMapper;
    using global::Streetcode.BLL.DTO.Users;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Users.Register;
    using Microsoft.AspNetCore.Identity;
    using Moq;
    using global::Streetcode.BLL.DTO.Users;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Users.Register;
    using global::Streetcode.DAL.Entities.Users;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class RegisterUserHandlerTests
    {
        private readonly Mock<UserManager<User>> userManagerMock;
        private readonly Mock<IMapper> mapperMock = new Mock<IMapper>();
        private readonly Mock<ILoggerService> loggerMock = new Mock<ILoggerService>();

        public RegisterUserHandlerTests()
        {
            var store = new Mock<IUserStore<User>>();
            this.userManagerMock = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        [Fact]
        public async Task Handle_ReturnsOkResult_WhenRegistrationIsSuccessful()
        {
            var dto = new RegisterUserDTO { Email = "test@test.com", Password = "Password123!" };
            var command = new RegisterUserCommand(dto);
            var user = new User { Email = "test@test.com" };

            this.mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            this.userManagerMock.Setup(u => u.CreateAsync(user, dto.Password)).ReturnsAsync(IdentityResult.Success);

            var handler = new RegisterUserHandler(this.userManagerMock.Object, this.mapperMock.Object, this.loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(dto.Email, result.Value.Email);
        }

        [Fact]
        public async Task Handle_ReturnsFailedResult_AndLogsError_WhenIdentityFails()
        {
            var dto = new RegisterUserDTO { Email = "test@test.com", Password = "123" };
            var command = new RegisterUserCommand(dto);
            var user = new User { Email = "test@test.com" };
            var identityError = new IdentityError { Description = "Password too short" };

            this.mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            this.userManagerMock.Setup(u => u.CreateAsync(user, dto.Password)).ReturnsAsync(IdentityResult.Failed(identityError));

            var handler = new RegisterUserHandler(this.userManagerMock.Object, this.mapperMock.Object, this.loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal("Password too short", result.Errors.First().Message);
            this.loggerMock.Verify(l => l.LogError(command, "Password too short"), Times.Once);
        }

        [Fact]
        public async Task Handle_AddsRole_WhenRoleIsProvidedAndUserCreated()
        {
            var dto = new RegisterUserDTO { Email = "test@test.com", Password = "Password123!", Role = "Admin" };
            var command = new RegisterUserCommand(dto);
            var user = new User { Email = "test@test.com" };

            this.mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            this.userManagerMock.Setup(u => u.CreateAsync(user, dto.Password)).ReturnsAsync(IdentityResult.Success);
            this.userManagerMock.Setup(u => u.AddToRoleAsync(user, dto.Role)).ReturnsAsync(IdentityResult.Success);

            var handler = new RegisterUserHandler(this.userManagerMock.Object, this.mapperMock.Object, this.loggerMock.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            this.userManagerMock.Verify(u => u.AddToRoleAsync(user, dto.Role), Times.Once);
        }
    }
}