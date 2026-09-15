// <copyright file="CreateReplyControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Controllers
{
    using System.Reflection;
    using System.Security.Claims;
    using FluentResults;
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Streetcode.BLL.DTO.Streetcode.Comments;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Reply;
    using Streetcode.WebApi.Controllers.Streetcode;
    using Xunit;

    public class CreateReplyControllerTests
    {
        [Fact]
        public async Task CreateReply_ShouldSendCommandWithCurrentUserAndReturnOk()
        {
            const int parentCommentId = 15;
            var authorId = Guid.NewGuid();
            var dto = new CreateCommentDto { Text = "Reply text" };
            var expectedDto = new CommentDto { Id = 20, Text = dto.Text };
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<CreateReplyCommand>(command =>
                        command.ParentCommentId == parentCommentId &&
                        command.AuthorId == authorId &&
                        command.Reply == dto),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(expectedDto));

            var controller = this.CreateController(mediatorMock.Object, authorId);

            var result = await controller.CreateReply(parentCommentId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expectedDto, okResult.Value);
            mediatorMock.VerifyAll();
        }

        [Fact]
        public async Task CreateReply_WhenUserIdClaimIsMissing_ShouldReturnUnauthorized()
        {
            var controller = new CommentController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };

            var result = await controller.CreateReply(15, new CreateCommentDto { Text = "Reply text" });

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public void CreateReply_ShouldHaveExpectedRouteAndRequireAuthorization()
        {
            MethodInfo? method = typeof(CommentController).GetMethod(nameof(CommentController.CreateReply));

            Assert.NotNull(method);
            var httpPostAttribute = method.GetCustomAttribute<HttpPostAttribute>();
            var authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(httpPostAttribute);
            Assert.Equal("{parentCommentId:int}", httpPostAttribute.Template);
            Assert.NotNull(authorizeAttribute);
        }

        private CommentController CreateController(IMediator mediator, Guid authorId)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton(mediator)
                .BuildServiceProvider();
            return new CommentController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = serviceProvider,
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, authorId.ToString())])),
                    },
                },
            };
        }
    }
}
