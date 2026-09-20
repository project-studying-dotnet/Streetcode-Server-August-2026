using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Application.Abstractions;

public interface IEmailDeliverySender
{
    Task SendAsync(
        EmailDelivery delivery,
        CancellationToken cancellationToken);
}
