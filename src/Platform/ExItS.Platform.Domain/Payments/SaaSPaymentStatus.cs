namespace ExItS.Platform.Domain.Payments;

public enum SaaSPaymentStatus
{
    PendingConfirmation = 1,
    Confirmed = 2,
    Rejected = 3,
    Voided = 4
}
