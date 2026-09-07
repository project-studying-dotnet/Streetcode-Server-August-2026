using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Application.Abstractions;

public interface IEmailDeliveryRepository
{
    Task<EmailDelivery?> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken);

    Task AddAsync(
        EmailDelivery emailDelivery,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
