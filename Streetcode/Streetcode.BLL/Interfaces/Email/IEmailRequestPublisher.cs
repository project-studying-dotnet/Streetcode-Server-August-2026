using Streetcode.Email.Contracts.Events;

namespace Streetcode.BLL.Interfaces.Email;

public interface IEmailRequestPublisher
{
    Task PublishAsync(
        EmailRequestedV1 emailRequested,
        CancellationToken cancellationToken);
}
