using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Streetcode.Email.Infrastructure.BackgroundJobs;

namespace Streetcode.Email.UnitTests.Infrastructure.BackgroundJobs;

public sealed class HangfireEmailJobSchedulerTests
{
    [Fact]
    public async Task EnqueueAsync_WithValidMessageId_CreatesSendJobAndFailureContinuation()
    {
        var messageId = Guid.NewGuid();
        var jobClient = new RecordingBackgroundJobClient();
        var scheduler = new HangfireEmailJobScheduler(jobClient);

        await scheduler.EnqueueAsync(
            messageId,
            CancellationToken.None);

        Assert.Equal(2, jobClient.CreatedJobs.Count);

        var (sendJob, sendState) = jobClient.CreatedJobs[0];

        Assert.Equal(typeof(SendEmailDeliveryJob), sendJob.Type);
        Assert.Equal(nameof(SendEmailDeliveryJob.ExecuteAsync), sendJob.Method.Name);
        Assert.Equal(messageId, Assert.IsType<Guid>(sendJob.Args[0]));
        Assert.Equal(
            CancellationToken.None,
            Assert.IsType<CancellationToken>(sendJob.Args[1]));
        Assert.IsType<EnqueuedState>(sendState);

        var (failureJob, continuationState) = jobClient.CreatedJobs[1];

        Assert.Equal(typeof(MarkEmailDeliveryAsFailedJob), failureJob.Type);
        Assert.Equal(
            nameof(MarkEmailDeliveryAsFailedJob.ExecuteAsync),
            failureJob.Method.Name);
        Assert.Equal(messageId, Assert.IsType<Guid>(failureJob.Args[0]));
        Assert.Equal(
            CancellationToken.None,
            Assert.IsType<CancellationToken>(failureJob.Args[1]));

        var awaitingState = Assert.IsType<AwaitingState>(continuationState);

        Assert.Equal("job-id-1", awaitingState.ParentId);
        Assert.Equal(
            JobContinuationOptions.OnlyOnDeletedState,
            awaitingState.Options);
        Assert.IsType<EnqueuedState>(awaitingState.NextState);
    }

    [Fact]
    public async Task EnqueueAsync_WithEmptyMessageId_ThrowsBeforeCreatingJob()
    {
        var jobClient = new RecordingBackgroundJobClient();
        var scheduler = new HangfireEmailJobScheduler(jobClient);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => scheduler.EnqueueAsync(
                Guid.Empty,
                CancellationToken.None));

        Assert.Equal("messageId", exception.ParamName);
        Assert.Empty(jobClient.CreatedJobs);
    }

    [Fact]
    public async Task EnqueueAsync_WithCancelledToken_DoesNotCreateJob()
    {
        var jobClient = new RecordingBackgroundJobClient();
        var scheduler = new HangfireEmailJobScheduler(jobClient);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scheduler.EnqueueAsync(
                Guid.NewGuid(),
                cancellationTokenSource.Token));

        Assert.Empty(jobClient.CreatedJobs);
    }

    [Fact]
    public async Task EnqueueAsync_WhenHangfireFails_PropagatesException()
    {
        var expectedException = new InvalidOperationException(
            "Hangfire storage is unavailable.");
        var jobClient = new RecordingBackgroundJobClient(expectedException);
        var scheduler = new HangfireEmailJobScheduler(jobClient);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scheduler.EnqueueAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Same(expectedException, actualException);
    }

    [Fact]
    public async Task EnqueueAsync_WhenContinuationCreationFails_PropagatesExceptionAfterCreatingSendJob()
    {
        var messageId = Guid.NewGuid();

        var expectedException = new InvalidOperationException(
            "Continuation storage is unavailable.");

        var jobClient = new RecordingBackgroundJobClient(
            expectedException,
            failOnCreateCall: 2);

        var scheduler = new HangfireEmailJobScheduler(jobClient);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scheduler.EnqueueAsync(
                messageId,
                CancellationToken.None));

        Assert.Same(expectedException, actualException);

        var (sendJob, sendState) = Assert.Single(
            jobClient.CreatedJobs);

        Assert.Equal(typeof(SendEmailDeliveryJob), sendJob.Type);
        Assert.Equal(
            nameof(SendEmailDeliveryJob.ExecuteAsync),
            sendJob.Method.Name);
        Assert.Equal(
            messageId,
            Assert.IsType<Guid>(sendJob.Args[0]));
        Assert.IsType<EnqueuedState>(sendState);
    }

    private sealed class RecordingBackgroundJobClient : IBackgroundJobClient
    {
        private readonly Exception? createException;
        private readonly int failOnCreateCall;
        private int createCallCount;

        public RecordingBackgroundJobClient(
            Exception? createException = null,
            int failOnCreateCall = 1)
        {
            this.createException = createException;
            this.failOnCreateCall = failOnCreateCall;
        }

        public List<(Job Job, IState State)> CreatedJobs { get; } = [];

        public string Create(Job job, IState state)
        {
            createCallCount++;

            if (createException is not null &&
                createCallCount == failOnCreateCall)
            {
                throw createException;
            }

            CreatedJobs.Add((job, state));

            return $"job-id-{CreatedJobs.Count}";
        }

        public bool ChangeState(
            string jobId,
            IState state,
            string expectedState)
        {
            throw new NotSupportedException();
        }
    }
}
