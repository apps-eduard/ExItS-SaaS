namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>
/// Authorization state for NEW Utang against a seller-owned POS customer.
/// No row / NotConfigured means new Utang is blocked. Outstanding remains ledger-derived.
/// </summary>
public enum CustomerCreditPolicyStatus
{
    /// <summary>Reserved read-model value when no policy row exists. Never persisted.</summary>
    NotConfigured = 0,

    PendingApproval = 1,
    Approved = 2,
    Disabled = 3
}
