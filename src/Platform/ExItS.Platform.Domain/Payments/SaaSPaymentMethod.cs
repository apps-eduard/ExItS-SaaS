namespace ExItS.Platform.Domain.Payments;

/// <summary>Manual reporting channel for a SaaS subscription payment. No gateway integration.</summary>
public enum SaaSPaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    GCash = 3
}
