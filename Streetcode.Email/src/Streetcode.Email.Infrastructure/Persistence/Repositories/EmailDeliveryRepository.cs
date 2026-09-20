using Microsoft.EntityFrameworkCore;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Infrastructure.Persistence.Repositories;

public sealed class EmailDeliveryRepository : IEmailDeliveryRepository
{
    private readonly EmailDbContext dbContext;

    public EmailDeliveryRepository(EmailDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<EmailDelivery?> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return dbContext.EmailDeliveries.SingleOrDefaultAsync(
            delivery => delivery.MessageId == messageId,
            cancellationToken);
    }

    public async Task AddAsync(
        EmailDelivery emailDelivery,
        CancellationToken cancellationToken)
    {
        await dbContext.EmailDeliveries.AddAsync(
            emailDelivery,
            cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
