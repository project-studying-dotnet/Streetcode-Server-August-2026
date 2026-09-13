using System.ComponentModel.DataAnnotations;

namespace Streetcode.Email.Infrastructure.EmailSending;

public sealed class FeedbackEmailOptions
{
    public const string SectionName = "EmailTemplates:Feedback";

    [Required]
    [EmailAddress]
    public string RecipientAddress { get; init; } = string.Empty;
}
