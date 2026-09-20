// <copyright file="ServiceCollectionExtensionsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcode.WebApi.Extensions;
using Xunit;

namespace Streetcode.XUnitTest.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationServices_ConfiguredCors_UsesConfiguredPolicy()
    {
        var allowedOrigins = new[]
        {
            "https://streetcode.example",
            "https://admin.streetcode.example",
        };
        var allowedHeaders = new[] { "Content-Type", "Authorization" };
        var allowedMethods = new[] { "GET", "POST" };
        const int preflightMaxAgeSeconds = 120;
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(
            allowedOrigins,
            allowedHeaders,
            allowedMethods,
            preflightMaxAgeSeconds);

        services.AddApplicationServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var corsOptions = serviceProvider
            .GetRequiredService<IOptions<CorsOptions>>()
            .Value;
        var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

        Assert.NotNull(policy);
        Assert.False(policy.AllowAnyOrigin);
        Assert.Equal(allowedOrigins, policy.Origins);
        Assert.Equal(allowedHeaders, policy.Headers);
        Assert.Equal(allowedMethods, policy.Methods);
        Assert.Equal(
            TimeSpan.FromSeconds(preflightMaxAgeSeconds),
            policy.PreflightMaxAge);
    }

    private static ConfigurationManager CreateConfiguration(
        IReadOnlyList<string> allowedOrigins,
        IReadOnlyList<string> allowedHeaders,
        IReadOnlyList<string> allowedMethods,
        int preflightMaxAgeSeconds)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=localhost;Database=Streetcode;" +
                "User Id=sa;Password=Password123!;" +
                "TrustServerCertificate=True;",
            ["CORS:PreflightMaxAge"] =
                preflightMaxAgeSeconds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
        };

        AddValues(values, "CORS:AllowedOrigins", allowedOrigins);
        AddValues(values, "CORS:AllowedHeaders", allowedHeaders);
        AddValues(values, "CORS:AllowedMethods", allowedMethods);

        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(values);
        return configuration;
    }

    private static void AddValues(
        IDictionary<string, string?> values,
        string section,
        IReadOnlyList<string> configuredValues)
    {
        for (var index = 0; index < configuredValues.Count; index++)
        {
            values[$"{section}:{index}"] = configuredValues[index];
        }
    }
}
