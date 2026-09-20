using Hangfire;
using Streetcode.Email.Application.Abstractions;

namespace Streetcode.Email.Infrastructure.BackgroundJobs;

public sealed class HangfireEmailJobScheduler : IEmailJobScheduler
{
    private readonly IBackgroundJobClient _jobClient;

    public HangfireEmailJobScheduler(IBackgroundJobClient jobClient)
    {
        _jobClient = jobClient;
    }

    public Task EnqueueAsync(Guid messageId, CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Message ID cannot be empty.",
                nameof(messageId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sendJobId = _jobClient.Enqueue<SendEmailDeliveryJob>(
            job => job.ExecuteAsync(
                messageId,
                CancellationToken.None));

        _jobClient.ContinueJobWith<MarkEmailDeliveryAsFailedJob>(
            sendJobId,
            job => job.ExecuteAsync(
                messageId,
                CancellationToken.None),
            JobContinuationOptions.OnlyOnDeletedState);

        return Task.CompletedTask;
    }
}
