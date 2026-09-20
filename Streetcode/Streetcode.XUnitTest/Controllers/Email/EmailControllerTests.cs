// <copyright file="EmailControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Streetcode.BLL.DTO.Email;
using Streetcode.BLL.MediatR.Email;
using Streetcode.WebApi.Controllers.Email;
using Xunit;

namespace Streetcode.XUnitTest.Controllers.Email;

public class EmailControllerTests
{
    [Fact]
    public async Task Send_Success_ReturnsAcceptedWithMessageIdAndForwardsRequest()
    {
        var messageId = Guid.NewGuid();
        var email = new EmailDTO
        {
            MessageId = messageId,
            From = "sender@example.com",
            Content = "Feedback content",
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mediatorMock = new Mock<IMediator>();

        mediatorMock
            .Setup(mediator => mediator.Send(
                It.Is<SendEmailCommand>(command => command.Email == email),
                cancellationToken))
            .ReturnsAsync(Result.Ok(messageId));

        using var serviceProvider = new ServiceCollection()
            .AddSingleton(mediatorMock.Object)
            .BuildServiceProvider();
        var controller = CreateController(serviceProvider);

        var result = await controller.Send(email, cancellationToken);

        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        var messageIdProperty = acceptedResult.Value?
            .GetType()
            .GetProperty("MessageId");
        Assert.NotNull(messageIdProperty);
        Assert.Equal(
            messageId,
            messageIdProperty.GetValue(acceptedResult.Value));
        mediatorMock.Verify(
            mediator => mediator.Send(
                It.Is<SendEmailCommand>(command => command.Email == email),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Send_Failure_ReturnsBadRequestAndInvokesMediatorOnce()
    {
        var email = new EmailDTO
        {
            MessageId = Guid.NewGuid(),
            From = "sender@example.com",
            Content = "Feedback content",
        };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(mediator => mediator.Send(
                It.Is<SendEmailCommand>(command => command.Email == email),
                CancellationToken.None))
            .ReturnsAsync(Result.Fail<Guid>("Publishing failed"));

        using var serviceProvider = new ServiceCollection()
            .AddSingleton(mediatorMock.Object)
            .BuildServiceProvider();
        var controller = CreateController(serviceProvider);

        var result = await controller.Send(email, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        mediatorMock.Verify(
            mediator => mediator.Send(
                It.Is<SendEmailCommand>(command => command.Email == email),
                CancellationToken.None),
            Times.Once);
    }

    private static EmailController CreateController(
        IServiceProvider serviceProvider)
    {
        return new EmailController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProvider,
                },
            },
        };
    }
}
