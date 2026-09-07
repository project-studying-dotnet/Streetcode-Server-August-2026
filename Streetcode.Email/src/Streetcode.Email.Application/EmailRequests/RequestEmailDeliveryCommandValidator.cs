using FluentValidation;
using Streetcode.Email.Application.EmailTemplates;

namespace Streetcode.Email.Application.EmailRequests;

public sealed class RequestEmailDeliveryCommandValidator
    : AbstractValidator<RequestEmailDeliveryCommand>
{
    private const int MaxTemplateLength = 100;
    private const int MaxTemplateDataEntries = 20;
    private const int MaxTemplateDataKeyLength = 100;
    private const int MaxTemplateDataValueLength = 10_000;
    private const int MaxTemplateDataTotalLength = 20_000;

    public RequestEmailDeliveryCommandValidator()
    {
        RuleFor(command => command.MessageId)
            .NotEmpty();

        RuleFor(command => command.CorrelationId)
            .NotEmpty();

        RuleFor(command => command.RequestedAtUtc)
            .NotEqual(default(DateTimeOffset))
            .Must(requestedAtUtc => requestedAtUtc.Offset == TimeSpan.Zero)
            .WithMessage("RequestedAtUtc must use the UTC offset.");

        RuleFor(command => command.Template)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(MaxTemplateLength)
            .Equal(FeedbackEmailTemplatePolicy.TemplateName)
            .WithMessage("Unsupported email template");

        RuleFor(command => command.TemplateData)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(templateData =>
                templateData!.Count <= MaxTemplateDataEntries)
            .WithMessage(
                $"TemplateData cannot contain more than {MaxTemplateDataEntries} entries.")
            .Must(templateData =>
                templateData!.Sum(pair =>
                    (pair.Key?.Length ?? 0) +
                    (pair.Value?.Length ?? 0))
                <= MaxTemplateDataTotalLength)
            .WithMessage(
                $"TemplateData cannot exceed {MaxTemplateDataTotalLength} characters.");

        RuleForEach(command => command.TemplateData!)
            .ChildRules(entry =>
            {
                entry.RuleFor(pair => pair.Key)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .MaximumLength(MaxTemplateDataKeyLength);

                entry.RuleFor(pair => pair.Value)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .MaximumLength(MaxTemplateDataValueLength);
            })
            .When(command => command.TemplateData is not null);

        When(
            command =>
                command.Template == FeedbackEmailTemplatePolicy.TemplateName,
            () =>
            {
                RuleFor(command => command.Recipient)
                    .Null()
                    .WithMessage(
                        "Recipient must not be provided for the feedback template.");

                RuleFor(command => command.TemplateData)
                    .Must(templateData =>
                        templateData is null ||
                        (templateData.Count ==
                            FeedbackEmailTemplatePolicy.RequiredTemplateDataCount &&
                         templateData.Keys.All(
                            FeedbackEmailTemplatePolicy.IsAllowedKey)))
                    .WithMessage(
                        "Feedback TemplateData must contain only From and Content.");

                RuleFor(command => GetTemplateDataValue(
                        command.TemplateData,
                        FeedbackEmailTemplatePolicy.SenderEmailKey))
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .MaximumLength(
                        FeedbackEmailTemplatePolicy.SenderEmailMaxLength)
                    .EmailAddress()
                    .OverridePropertyName(
                        $"{nameof(RequestEmailDeliveryCommand.TemplateData)}." +
                        FeedbackEmailTemplatePolicy.SenderEmailKey)
                    .When(command => command.TemplateData is not null);

                RuleFor(command => GetTemplateDataValue(
                        command.TemplateData,
                        FeedbackEmailTemplatePolicy.ContentKey))
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .MaximumLength(
                        FeedbackEmailTemplatePolicy.ContentMaxLength)
                    .OverridePropertyName(
                        $"{nameof(RequestEmailDeliveryCommand.TemplateData)}." +
                        FeedbackEmailTemplatePolicy.ContentKey)
                    .When(command => command.TemplateData is not null);
            });
    }

    private static string? GetTemplateDataValue(
        IReadOnlyDictionary<string, string>? templateData,
        string key)
    {
        return templateData is not null &&
               templateData.TryGetValue(key, out var value)
            ? value
            : null;
    }
}
