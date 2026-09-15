// <copyright file="CommentControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Controllers
{
    using System.Reflection;
    using FluentResults;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Streetcode.BLL.DTO.Streetcode.Comments;
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
            var dto = new UpdateCommentDto { Text = "Updated comment" };
            var expectedDto = new CommentDto { Id = commentId, Text = dto.Text };
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(mediator => mediator.Send(
                    It.Is<UpdateCommentCommand>(command =>
                        command.Id == commentId && command.Comment == dto),
                    It.IsAny<CancellationToken>()))
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
                    },
                },
            };

            var result = await controller.Update(commentId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expectedDto, okResult.Value);
            mediatorMock.VerifyAll();
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

            Assert.NotNull(httpPutAttribute);
            Assert.Equal("{id:int}", httpPutAttribute.Template);
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
