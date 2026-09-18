namespace Streetcode.BLL.Exceptions;

public sealed class EmailRequestPublishingException : Exception
{
    public EmailRequestPublishingException(string message)
        : base(message)
    {
    }

    public EmailRequestPublishingException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
