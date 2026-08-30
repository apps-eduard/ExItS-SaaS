namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>Lifecycle of an organization-scoped supplier payable (ADR-023).</summary>
public enum SupplierPayableStatus
{
    Open = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Voided = 3
}
