using System.ComponentModel.DataAnnotations;

namespace Streetcode.Email.Infrastructure.EmailSending;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool UseSsl { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required]
    [EmailAddress]
    public string SenderAddress { get; init; } = string.Empty;
}
