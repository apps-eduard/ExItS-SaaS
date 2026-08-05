namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>POS electronic / simulated payment attempt lifecycle.</summary>
public enum PaymentAttemptStatus
{
    Created = 0,
    Pending = 1,
    RequiresCustomerAction = 2,
    Processing = 3,
    Paid = 4,
    Failed = 5,
    Cancelled = 6,
    Expired = 7,
    Refunded = 8,
    PendingManualVerification = 9
}

/// <summary>Payment method for a payment attempt (provider-ready).</summary>
public enum PaymentAttemptMethod
{
    Cash = 0,
    Card = 1,
    GCash = 2,
    ManualGCashTransfer = 3
}

/// <summary>Known providers. <see cref="Fake"/> is development/simulation only.</summary>
public enum PaymentProvider
{
    None = 0,
    Fake = 1,
    Manual = 2
}
