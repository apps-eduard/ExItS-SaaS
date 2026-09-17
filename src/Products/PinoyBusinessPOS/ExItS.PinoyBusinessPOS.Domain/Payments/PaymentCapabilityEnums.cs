namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>Plan-tier payment capability (checked via feature codes — never planKey string compares).</summary>
public enum PaymentCapability
{
    BasicPayments = 0,
    PaymentManagement = 1,
    OnlinePayments = 2
}

/// <summary>How a tender is collected relative to a payment provider.</summary>
public enum PaymentIntegrationMode
{
    None = 0,
    Manual = 1,
    Online = 2
}

/// <summary>When funds are considered settled for the tender.</summary>
public enum PaymentSettlementMode
{
    Immediate = 0,
    Deferred = 1,
    ExternalConfirmation = 2
}

/// <summary>UI/runtime availability of a catalog payment channel.</summary>
public enum PaymentMethodAvailability
{
    BuiltIn = 0,
    Configurable = 1,
    ComingSoon = 2
}

/// <summary>Branch scope for a configurable manual payment method.</summary>
public enum PaymentMethodBranchScope
{
    AllBranches = 0,
    SelectedBranches = 1
}
