namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Simple retail sale lifecycle. Member names are the stable persistence codes.
/// A completed sale is never edited; corrections use an explicit void with a reason.
/// </summary>
public enum SaleStatus
{
    Completed = 0,
    Voided = 1
}
