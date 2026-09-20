namespace Streetcode.Email.Application.Abstractions;

public interface IEmailJobScheduler
{
    Task EnqueueAsync(
        Guid messageId,
        CancellationToken cancellationToken);
}
