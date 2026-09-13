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

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string SenderAddress { get; init; } = string.Empty;
}
