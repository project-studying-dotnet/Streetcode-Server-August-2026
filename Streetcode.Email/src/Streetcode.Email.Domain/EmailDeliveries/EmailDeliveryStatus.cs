namespace Streetcode.Email.Domain.EmailDeliveries;

public enum EmailDeliveryStatus
{
    Pending,
    Sending,
    Sent,
    Failed,
    DeliveryUncertain
}
