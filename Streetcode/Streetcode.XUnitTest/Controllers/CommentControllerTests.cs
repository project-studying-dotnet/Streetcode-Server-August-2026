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
    using Streetcode.BLL.MediatR.Streetcode.Comment.Update;
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
        public void Update_ShouldHaveExpectedRoute()
        {
            MethodInfo? method = typeof(CommentController).GetMethod(nameof(CommentController.Update));

            Assert.NotNull(method);
            var httpPutAttribute = method.GetCustomAttribute<HttpPutAttribute>();

            Assert.NotNull(httpPutAttribute);
            Assert.Equal("{id:int}", httpPutAttribute.Template);
        }
    }
}
