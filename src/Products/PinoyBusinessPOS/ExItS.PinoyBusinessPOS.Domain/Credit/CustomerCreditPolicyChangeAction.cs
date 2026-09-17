namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Append-only policy mutation kinds (one meaningful row per business mutation).</summary>
public enum CustomerCreditPolicyChangeAction
{
    Configured = 0,
    CreditLimitChanged = 1,
    TermChanged = 2,
    CreditLimitAndTermChanged = 3,
    Approved = 4,
    Disabled = 5,
    Reconfigured = 6
}
