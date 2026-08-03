namespace ExItS.Platform.Domain.Payments;

/// <summary>
/// Channel for a SaaS subscription payment visible in Platform Administration → Payments.
/// <see cref="Online"/> covers Local Validation / provider charges; other values are manual attestation.
/// </summary>
public enum SaaSPaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    GCash = 3,
    Online = 4
}
