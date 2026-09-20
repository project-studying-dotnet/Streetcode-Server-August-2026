// <copyright file="EmailKafkaServiceCollectionExtensionsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcode.BLL.Interfaces.Email;
using Streetcode.WebApi.Kafka;
using Xunit;

namespace Streetcode.XUnitTest.Kafka;

public class EmailKafkaServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEmailKafka_NullServices_ThrowsArgumentNullException()
    {
        var configuration = CreateConfiguration(
            "localhost:9092",
            "email.requests");

        var exception = Assert.Throws<ArgumentNullException>(
            () => EmailKafkaServiceCollectionExtensions.AddEmailKafka(
                null!,
                configuration));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddEmailKafka_NullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentNullException>(
            () => services.AddEmailKafka(null!));

        Assert.Equal("configuration", exception.ParamName);
    }

    [Fact]
    public void AddEmailKafka_MissingSection_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddEmailKafka(configuration));

        Assert.Contains(EmailKafkaOptions.SectionName, exception.Message);
    }

    [Fact]
    public void AddEmailKafka_ValidConfiguration_BindsOptionsAndRegistersSingletons()
    {
        const string bootstrapServers = "localhost:9092";
        const string topic = "email.requests";
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration(bootstrapServers, topic);

        var returnedServices = services.AddEmailKafka(configuration);

        Assert.Same(services, returnedServices);
        AssertSingletonRegistration<IProducer<string, string>>(services);
        var publisherDescriptor = AssertSingletonRegistration<IEmailRequestPublisher>(
            services);
        Assert.Equal(
            typeof(KafkaEmailRequestPublisher),
            publisherDescriptor.ImplementationType);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptions<EmailKafkaOptions>>()
            .Value;
        Assert.Equal(bootstrapServers, options.BootstrapServers);
        Assert.Equal(topic, options.Topic);

        var firstProducer = serviceProvider
            .GetRequiredService<IProducer<string, string>>();
        var secondProducer = serviceProvider
            .GetRequiredService<IProducer<string, string>>();
        Assert.Same(firstProducer, secondProducer);

        var firstPublisher = serviceProvider
            .GetRequiredService<IEmailRequestPublisher>();
        var secondPublisher = serviceProvider
            .GetRequiredService<IEmailRequestPublisher>();
        Assert.IsType<KafkaEmailRequestPublisher>(firstPublisher);
        Assert.Same(firstPublisher, secondPublisher);
    }

    [Theory]
    [InlineData(null, "email.requests")]
    [InlineData("", "email.requests")]
    [InlineData("localhost:9092", null)]
    [InlineData("localhost:9092", "")]
    public void AddEmailKafka_MissingOrEmptyRequiredOption_ThrowsOptionsValidationException(
        string? bootstrapServers,
        string? topic)
    {
        var services = new ServiceCollection();
        var configurationValues = new Dictionary<string, string?>();
        if (bootstrapServers is not null)
        {
            configurationValues[$"{EmailKafkaOptions.SectionName}:BootstrapServers"] =
                bootstrapServers;
        }

        if (topic is not null)
        {
            configurationValues[$"{EmailKafkaOptions.SectionName}:Topic"] = topic;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        services.AddEmailKafka(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider
                .GetRequiredService<IOptions<EmailKafkaOptions>>()
                .Value);
    }

    private static IConfiguration CreateConfiguration(
        string bootstrapServers,
        string topic)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{EmailKafkaOptions.SectionName}:BootstrapServers"] =
                    bootstrapServers,
                [$"{EmailKafkaOptions.SectionName}:Topic"] = topic,
            })
            .Build();
    }

    private static ServiceDescriptor AssertSingletonRegistration<TService>(
        IServiceCollection services)
    {
        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(TService));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        return descriptor;
    }
}
