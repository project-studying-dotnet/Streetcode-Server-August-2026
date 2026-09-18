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
    using Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<CreateReplyCommand>(command =>
                        command.ParentCommentId == parentCommentId &&
                        command.AuthorId == authorId &&
                        command.Reply == dto),
                    It.Is<CancellationToken>(token => token == cancellationToken)))
                .ReturnsAsync(Result.Ok(expectedDto));

            var controller = CreateController(mediatorMock.Object, authorId);

            var result = await controller.CreateReply(parentCommentId, dto, cancellationToken);

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

            var result = await controller.CreateReply(
                15,
                new CreateCommentDto { Text = "Reply text" },
                CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task CreateReply_WhenParentDoesNotExist_ShouldReturnNotFound()
        {
            const int parentCommentId = 15;
            var authorId = Guid.NewGuid();
            var dto = new CreateCommentDto { Text = "Reply text" };
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<CreateReplyCommand>(command => command.ParentCommentId == parentCommentId),
                    CancellationToken.None))
                .ReturnsAsync(Result.Fail<CommentDto>(new CommentNotFoundError(parentCommentId)));
            var controller = CreateController(mediatorMock.Object, authorId);

            var result = await controller.CreateReply(parentCommentId, dto, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
            mediatorMock.VerifyAll();
        }

        [Fact]
        public void CreateReply_ShouldHaveExpectedRouteAndRequireAuthorization()
        {
            MethodInfo? method = typeof(CommentController).GetMethod(nameof(CommentController.CreateReply));

            Assert.NotNull(method);
            var httpPostAttribute = method.GetCustomAttribute<HttpPostAttribute>();
            var authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();
            var responseStatusCodes = method
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Select(attribute => attribute.StatusCode)
                .ToHashSet();

            Assert.NotNull(httpPostAttribute);
            Assert.Equal("{parentCommentId:int}", httpPostAttribute.Template);
            Assert.NotNull(authorizeAttribute);
            Assert.Contains(StatusCodes.Status200OK, responseStatusCodes);
            Assert.Contains(StatusCodes.Status400BadRequest, responseStatusCodes);
            Assert.Contains(StatusCodes.Status401Unauthorized, responseStatusCodes);
            Assert.Contains(StatusCodes.Status403Forbidden, responseStatusCodes);
            Assert.Contains(StatusCodes.Status404NotFound, responseStatusCodes);
        }

        private static CommentController CreateController(IMediator mediator, Guid authorId)
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
