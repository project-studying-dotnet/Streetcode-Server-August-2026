using System.Reflection;
using Hangfire;
using Streetcode.Email.Infrastructure.BackgroundJobs;

namespace Streetcode.Email.UnitTests.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobPolicyTests
{
    [Fact]
    public void MarkFailedJob_HasRetryAndConcurrencyPolicies()
    {
        var executeMethod = typeof(MarkEmailDeliveryAsFailedJob)
            .GetMethod(nameof(MarkEmailDeliveryAsFailedJob.ExecuteAsync));

        Assert.NotNull(executeMethod);
        Assert.NotNull(
            executeMethod.GetCustomAttribute<AutomaticRetryAttribute>());
        Assert.NotNull(
            executeMethod.GetCustomAttribute<
                DisableConcurrentExecutionAttribute>());
    }
}
