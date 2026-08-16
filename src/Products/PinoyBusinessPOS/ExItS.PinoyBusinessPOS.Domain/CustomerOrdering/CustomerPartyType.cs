namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Customer identity kind on a customer order.
/// Guest checkout is reserved for a future work package and is not valid in V1.
/// </summary>
public enum CustomerPartyType
{
    Personal = 1,
    Organization = 2
    // Guest = 3 — future
}
