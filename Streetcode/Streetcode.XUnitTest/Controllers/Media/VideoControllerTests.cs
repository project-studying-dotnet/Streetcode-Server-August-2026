using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Streetcode.BLL.DTO.Media.Video;
using Streetcode.BLL.MediatR.Media.Video.Create;
using Streetcode.WebApi.Controllers.Media;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Streetcode.XUnitTest.Controllers.Media;

public class VideoControllerTests
{
    [Fact]
    public async Task Create_SendsCreateVideoCommand_WhenUrlIsValid()
    {
        var mediatorMock = new Mock<IMediator>();

        var video = new VideoDTO
        {
            Url = "https://www.youtube.com/watch?v=test",
            Description = "Test video",
            StreetcodeId = 1
        };

        mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateVideoCommand>(c => c.Video == video),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FluentResults.Result.Ok(video));

        var controller = new VideoController();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var services = new ServiceCollection();
        services.AddSingleton(mediatorMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider()
            }
        };

        var result = await controller.Create(video);

        Assert.IsType<OkObjectResult>(result);

        mediatorMock.Verify(
            m => m.Send(
                It.Is<CreateVideoCommand>(c => c.Video == video),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}