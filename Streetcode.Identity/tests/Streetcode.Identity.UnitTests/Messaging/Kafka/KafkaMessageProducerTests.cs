using Confluent.Kafka;
using Moq;
using Streetcode.Identity.Infrastructure.Messaging.Kafka;

namespace Streetcode.Identity.UnitTests.Messaging.Kafka;

public sealed class KafkaMessageProducerTests
{
    [Fact]
    public async Task PublishAsync_WhenArgumentsAreValid_ShouldForwardMessageToProducer()
    {
        const string topic = "identity.user-access-changed.v1";
        const string key = "11111111-1111-1111-1111-111111111111";
        const string payload = "{\"isActive\":false}";

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken = cancellationTokenSource.Token;

        var producerMock =
            new Mock<IProducer<string, string>>(
                MockBehavior.Strict);

        producerMock
            .Setup(producer => producer.ProduceAsync(
                topic,
                It.Is<Message<string, string>>(message =>
                    message.Key == key &&
                    message.Value == payload),
                cancellationToken))
            .ReturnsAsync(new DeliveryResult<string, string>());

        var sut = new KafkaMessageProducer(
            producerMock.Object);

        await sut.PublishAsync(
            topic,
            key,
            payload,
            cancellationToken);

        producerMock.Verify(
            producer => producer.ProduceAsync(
                topic,
                It.Is<Message<string, string>>(message =>
                    message.Key == key &&
                    message.Value == payload),
                cancellationToken),
            Times.Once);

        producerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(
        " ",
        "11111111-1111-1111-1111-111111111111",
        "{\"isActive\":false}",
        "topic")]
    [InlineData(
        "identity.user-access-changed.v1",
        " ",
        "{\"isActive\":false}",
        "key")]
    [InlineData(
        "identity.user-access-changed.v1",
        "11111111-1111-1111-1111-111111111111",
        " ",
        "payload")]
    public async Task PublishAsync_WhenArgumentIsWhitespace_ShouldThrowAndNotCallProducer(
        string topic,
        string key,
        string payload,
        string expectedParameterName)
    {
        var producerMock =
            new Mock<IProducer<string, string>>(
                MockBehavior.Strict);

        var sut = new KafkaMessageProducer(
            producerMock.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.PublishAsync(
                topic,
                key,
                payload,
                CancellationToken.None));

        Assert.Equal(expectedParameterName, exception.ParamName);
        producerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishAsync_WhenProducerFails_ShouldPropagateProduceException()
    {
        const string topic = "identity.user-access-changed.v1";
        const string key = "11111111-1111-1111-1111-111111111111";
        const string payload = "{\"isActive\":false}";

        var expectedException =
            new ProduceException<string, string>(
                new Error(
                    ErrorCode.Local_Transport,
                    "Kafka broker unavailable"),
                new DeliveryResult<string, string>());

        var producerMock =
            new Mock<IProducer<string, string>>(
                MockBehavior.Strict);

        producerMock
            .Setup(producer => producer.ProduceAsync(
                topic,
                It.IsAny<Message<string, string>>(),
                CancellationToken.None))
            .ThrowsAsync(expectedException);

        var sut = new KafkaMessageProducer(
            producerMock.Object);

        var actualException =
            await Assert.ThrowsAsync<ProduceException<string, string>>(
                () => sut.PublishAsync(
                    topic,
                    key,
                    payload,
                    CancellationToken.None));

        Assert.Same(expectedException, actualException);

        producerMock.Verify(
            producer => producer.ProduceAsync(
                topic,
                It.IsAny<Message<string, string>>(),
                CancellationToken.None),
            Times.Once);

        producerMock.VerifyNoOtherCalls();
    }
}
