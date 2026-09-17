// <copyright file="CommentControllerTests.cs" company="PlaceholderCompany">
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
    using Streetcode.BLL.MediatR.ResultVariations;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
    using Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
    using Streetcode.BLL.MediatR.Streetcode.Comment.Update;
    using Streetcode.WebApi.Attributes;
    using Streetcode.WebApi.Controllers.Streetcode;
    using Xunit;

    public class CommentControllerTests
    {
        [Fact]
        public async Task Update_ShouldSendCommandAndReturnOk()
        {
            const int commentId = 15;
            var authorId = Guid.NewGuid();
            var dto = new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } };
            var expectedDto = new CommentDto { Id = commentId, Text = dto.Text };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<UpdateCommentCommand>(command =>
                        command.Id == commentId && command.AuthorId == authorId && command.Comment == dto),
                    cancellationToken))
                .ReturnsAsync(Result.Ok(expectedDto));

            using var serviceProvider = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider();
            var controller = new CommentController
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

            var result = await controller.Update(commentId, dto, cancellationToken);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expectedDto, okResult.Value);
            mediatorMock.VerifyAll();
        }

        [Fact]
        public async Task Update_WhenUserIdClaimIsMissing_ShouldReturnUnauthorized()
        {
            var controller = new CommentController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };

            var result = await controller.Update(
                15,
                new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } },
                CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Theory]
        [InlineData("NotFound", typeof(Microsoft.AspNetCore.Mvc.NotFoundResult), 404)]
        [InlineData("Forbidden", typeof(StatusCodeResult), 403)]
        [InlineData("Conflict", typeof(Microsoft.AspNetCore.Mvc.ConflictResult), 409)]
        public async Task Update_WhenHandlerReturnsStatusResult_ShouldReturnExpectedHttpStatus(
            string resultName,
            Type actionResultType,
            int expectedStatusCode)
        {
            const int commentId = 15;
            var authorId = Guid.NewGuid();
            var dto = new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } };
            var mediatorMock = new Mock<IMediator>();
            Result<CommentDto> handlerResult = resultName switch
            {
                "NotFound" => new NotFoundResult<CommentDto>(new Error("Not found")),
                "Forbidden" => new ForbiddenResult<CommentDto>(new Error("Forbidden")),
                "Conflict" => new ConflictResult<CommentDto>(new Error("Conflict")),
                _ => throw new ArgumentOutOfRangeException(nameof(resultName)),
            };
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<UpdateCommentCommand>(command =>
                        command.Id == commentId && command.AuthorId == authorId && command.Comment == dto),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResult);

            using var serviceProvider = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider();
            var controller = new CommentController
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

            var result = await controller.Update(commentId, dto, CancellationToken.None);

            Assert.IsType(actionResultType, result);
            var statusCodeResult = Assert.IsAssignableFrom<StatusCodeResult>(result);
            Assert.Equal(expectedStatusCode, statusCodeResult.StatusCode);
            mediatorMock.VerifyAll();
        }

        [Fact]
        public async Task Delete_ShouldSendCommandWithCancellationTokenAndReturnOk()
        {
            const int commentId = 15;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<DeleteCommentCommand>(command => command.Id == commentId),
                    cancellationToken))
                .ReturnsAsync(Result.Ok(Unit.Value));

            using var serviceProvider = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider();
            var controller = new CommentController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = serviceProvider,
                    },
                },
            };

            var result = await controller.Delete(commentId, cancellationToken);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(Unit.Value, okResult.Value);
            mediatorMock.VerifyAll();
        }

        [Fact]
        public async Task Delete_WhenCommentDoesNotExist_ShouldReturnNotFound()
        {
            const int commentId = 404;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var error = new CommentNotFoundError(commentId);
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<DeleteCommentCommand>(command => command.Id == commentId),
                    cancellationToken))
                .ReturnsAsync(Result.Fail<Unit>(error));

            using var serviceProvider = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider();
            var controller = new CommentController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = serviceProvider,
                    },
                },
            };

            var result = await controller.Delete(commentId, cancellationToken);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var reasons = Assert.IsAssignableFrom<IEnumerable<IReason>>(notFoundResult.Value);
            Assert.Contains(reasons, reason => reason.Message == error.Message);
            mediatorMock.VerifyAll();
        }

        [Fact]
        public void Delete_ShouldHaveExpectedEndpointMetadata()
        {
            MethodInfo? method = typeof(CommentController).GetMethod(nameof(CommentController.Delete));

            Assert.NotNull(method);
            var httpDeleteAttribute = method.GetCustomAttribute<HttpDeleteAttribute>();
            var authorizeAttribute = method.GetCustomAttribute<AuthorizeRoles>();
            var responseTypes = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();

            Assert.NotNull(httpDeleteAttribute);
            Assert.Equal("{id:int}", httpDeleteAttribute.Template);
            Assert.NotNull(authorizeAttribute);
            Assert.Equal(
                "MainAdministrator,Admin,Moderator",
                authorizeAttribute.Roles);
            Assert.Contains(responseTypes, attribute => attribute.StatusCode == StatusCodes.Status200OK);
            Assert.Contains(responseTypes, attribute => attribute.StatusCode == StatusCodes.Status400BadRequest);
            Assert.Contains(responseTypes, attribute => attribute.StatusCode == StatusCodes.Status401Unauthorized);
            Assert.Contains(responseTypes, attribute => attribute.StatusCode == StatusCodes.Status403Forbidden);
            Assert.Contains(responseTypes, attribute => attribute.StatusCode == StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task GetById_ShouldSendQueryWithCancellationTokenAndReturnOk()
        {
            const int commentId = 15;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var dto = new CommentWithRepliesDto { Id = commentId };
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<GetCommentByIdQuery>(query => query.Id == commentId),
                    cancellationToken))
                .ReturnsAsync(Result.Ok(dto));

            using var serviceProvider = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider();
            var controller = new CommentController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = serviceProvider,
                    },
                },
            };

            var result = await controller.GetById(commentId, cancellationToken);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(dto, okResult.Value);
            mediatorMock.VerifyAll();
        }

        [Fact]
        public void Update_ShouldHaveExpectedRoute()
        {
            MethodInfo? method = typeof(CommentController).GetMethod(nameof(CommentController.Update));

            Assert.NotNull(method);
            var httpPutAttribute = method.GetCustomAttribute<HttpPutAttribute>();
            var authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(httpPutAttribute);
            Assert.Equal("{id:int}", httpPutAttribute.Template);
            Assert.NotNull(authorizeAttribute);
        }

        [Fact]
        public void GetById_ShouldHaveExpectedRouteAndReviewRoles()
        {
            MethodInfo? method = typeof(CommentController).GetMethod(nameof(CommentController.GetById));

            Assert.NotNull(method);
            var httpGetAttribute = method.GetCustomAttribute<HttpGetAttribute>();
            var authorizeAttribute = method.GetCustomAttribute<AuthorizeRoles>();

            Assert.NotNull(httpGetAttribute);
            Assert.Equal("{id:int}", httpGetAttribute.Template);
            Assert.NotNull(authorizeAttribute);
            Assert.Equal(
                "MainAdministrator,Admin,Moderator",
                authorizeAttribute.Roles);
        }
    }
}
