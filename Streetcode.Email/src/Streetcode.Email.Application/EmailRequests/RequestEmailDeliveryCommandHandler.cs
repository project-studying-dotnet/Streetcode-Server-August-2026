using FluentValidation;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Application.EmailRequests;

public sealed class RequestEmailDeliveryCommandHandler
{
    private readonly IValidator<RequestEmailDeliveryCommand> validator;
    private readonly IEmailDeliveryRepository repository;
    private readonly IEmailJobScheduler jobScheduler;

    public RequestEmailDeliveryCommandHandler(
        IValidator<RequestEmailDeliveryCommand> validator,
        IEmailDeliveryRepository repository,
        IEmailJobScheduler jobScheduler)
    {
        this.validator = validator;
        this.repository = repository;
        this.jobScheduler = jobScheduler;
    }

    public async Task HandleAsync(
        RequestEmailDeliveryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(
            command,
            cancellationToken);

        var delivery = await repository.GetByMessageIdAsync(
            command.MessageId,
            cancellationToken);

        if (delivery is null)
        {
            delivery = new EmailDelivery(
                command.MessageId,
                command.CorrelationId,
                command.RequestedAtUtc,
                command.Template,
                command.Recipient,
                command.TemplateData!);

            await repository.AddAsync(
                delivery,
                cancellationToken);

            await repository.SaveChangesAsync(cancellationToken);
        }
        else if (!HasSameRequestData(delivery, command))
        {
            throw new EmailRequestConflictException(
                command.MessageId);
        }

        if (delivery.Status != EmailDeliveryStatus.Pending ||
            delivery.IsJobScheduled)
        {
            return;
        }

        await jobScheduler.EnqueueAsync(
            delivery.MessageId,
            cancellationToken);

        delivery.MarkJobAsScheduled();

        await repository.SaveChangesAsync(cancellationToken);
    }

    private static bool HasSameRequestData(
        EmailDelivery delivery,
        RequestEmailDeliveryCommand command)
    {
        var templateData = command.TemplateData!;

        return delivery.MessageId == command.MessageId &&
               delivery.CorrelationId == command.CorrelationId &&
               delivery.RequestedAtUtc == command.RequestedAtUtc &&
               string.Equals(
                   delivery.Template,
                   command.Template,
                   StringComparison.Ordinal) &&
               string.Equals(
                   delivery.Recipient,
                   command.Recipient,
                   StringComparison.Ordinal) &&
               delivery.TemplateData.Count == templateData.Count &&
               delivery.TemplateData.All(pair =>
                   templateData.TryGetValue(pair.Key, out var value) &&
                   string.Equals(
                       pair.Value,
                       value,
                       StringComparison.Ordinal));
    }
}
