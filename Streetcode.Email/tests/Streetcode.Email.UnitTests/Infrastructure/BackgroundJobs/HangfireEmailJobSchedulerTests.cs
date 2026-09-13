using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Streetcode.Email.Infrastructure.BackgroundJobs;

namespace Streetcode.Email.UnitTests.Infrastructure.BackgroundJobs;

public sealed class HangfireEmailJobSchedulerTests
{
    [Fact]
    public async Task EnqueueAsync_WithValidMessageId_CreatesExpectedHangfireJob()
    {
        var messageId = Guid.NewGuid();
        var jobClient = new RecordingBackgroundJobClient();
        var scheduler = new HangfireEmailJobScheduler(jobClient);

        await scheduler.EnqueueAsync(
            messageId,
            CancellationToken.None);

        var job = Assert.IsType<Job>(jobClient.CreatedJob);
        Assert.Equal(typeof(SendEmailDeliveryJob), job.Type);
        Assert.Equal(nameof(SendEmailDeliveryJob.ExecuteAsync), job.Method.Name);
        Assert.Equal(messageId, Assert.IsType<Guid>(job.Args[0]));
        Assert.Equal(
            CancellationToken.None,
            Assert.IsType<CancellationToken>(job.Args[1]));
        Assert.IsType<EnqueuedState>(jobClient.CreatedState);
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
        Assert.Null(jobClient.CreatedJob);
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

        Assert.Null(jobClient.CreatedJob);
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

    private sealed class RecordingBackgroundJobClient : IBackgroundJobClient
    {
        private readonly Exception? createException;

        public RecordingBackgroundJobClient(Exception? createException = null)
        {
            this.createException = createException;
        }

        public Job? CreatedJob { get; private set; }

        public IState? CreatedState { get; private set; }

        public string Create(Job job, IState state)
        {
            if (createException is not null)
            {
                throw createException;
            }

            CreatedJob = job;
            CreatedState = state;

            return "job-id";
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
