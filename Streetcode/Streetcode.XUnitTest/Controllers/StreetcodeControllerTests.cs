using System.Reflection;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Streetcode.BLL.DTO.Streetcode.RelatedFigure;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetByIds;
using Streetcode.WebApi.Controllers.Streetcode;
using Xunit;

namespace Streetcode.XUnitTest.Controllers;

public class StreetcodeControllerTests
{
    [Fact]
    public async Task GetByIds_ShouldSendQueryWithIdsAndCancellationTokenAndReturnOk()
    {
        int[] ids = { 3, 1, 2 };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var streetcodes = new List<RelatedFigureDTO> { new() { Id = 3 }, new() { Id = 1 } };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(mediator => mediator.Send(
                It.Is<GetStreetcodesByIdsQuery>(query => query.Ids.SequenceEqual(ids)),
                cancellationToken))
            .ReturnsAsync(Result.Ok<IEnumerable<RelatedFigureDTO>>(streetcodes));
        var controller = CreateController(mediatorMock);

        var result = await controller.GetByIds(ids, cancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(streetcodes, okResult.Value);
        mediatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetByIds_WhenQueryFails_ShouldReturnBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(mediator => mediator.Send(
                It.IsAny<GetStreetcodesByIdsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<IEnumerable<RelatedFigureDTO>>("Cannot find any streetcodes by the given ids"));
        var controller = CreateController(mediatorMock);

        var result = await controller.GetByIds(new[] { 1 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetByIds_ShouldBeAnonymousGetEndpointBoundFromQuery()
    {
        MethodInfo? method = typeof(StreetcodeController).GetMethod(nameof(StreetcodeController.GetByIds));

        Assert.NotNull(method);
        Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>());
        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        Assert.Empty(typeof(StreetcodeController).GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        Assert.NotNull(method.GetParameters().First().GetCustomAttribute<FromQueryAttribute>());
    }

    [Fact]
    public void GetByIds_ShouldDeclareExpectedResponseTypes()
    {
        MethodInfo? method = typeof(StreetcodeController).GetMethod(nameof(StreetcodeController.GetByIds));

        Assert.NotNull(method);
        var responseTypes = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();

        Assert.Contains(
            responseTypes,
            attribute => attribute.StatusCode == StatusCodes.Status200OK
                && attribute.Type == typeof(IEnumerable<RelatedFigureDTO>));
        Assert.Contains(responseTypes, attribute => attribute.StatusCode == StatusCodes.Status400BadRequest);
    }

    private static StreetcodeController CreateController(Mock<IMediator> mediatorMock)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton(mediatorMock.Object)
            .BuildServiceProvider();

        return new StreetcodeController
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
