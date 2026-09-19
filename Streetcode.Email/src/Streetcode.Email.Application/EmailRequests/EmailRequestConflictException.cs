namespace Streetcode.Email.Application.EmailRequests;

public sealed class EmailRequestConflictException : Exception
{
    public Guid MessageId { get; }

    public EmailRequestConflictException(Guid messageId)
        : base(
            $"Email request '{messageId}' conflicts with " +
            "the existing request data.")
    {
        MessageId = messageId;
    }
}
