namespace Streetcode.Email.Application.EmailSending;

public sealed class EmailDeliveryOutcomeUnknownException : Exception
{
    public EmailDeliveryOutcomeUnknownException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
