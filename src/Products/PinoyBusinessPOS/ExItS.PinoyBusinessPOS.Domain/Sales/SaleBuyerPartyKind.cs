namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Sale buyer/counterparty kind. Does not determine sale ownership —
/// the selling <c>OrganizationId</c> always owns the sale.
/// </summary>
public enum SaleBuyerPartyKind
{
    WalkIn = 0,
    ExternalCustomer = 1,
    Personal = 2,
    Organization = 3
}
