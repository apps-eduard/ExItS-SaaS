namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Inventory hold lifecycle for provider-backed electronic sales (Card/GCash).
/// Cash/Utang/ManualGCash stay <see cref="None"/> and deduct immediately at checkout.
/// </summary>
public enum SaleStockReservationState
{
    None = 0,
    Reserved = 1,
    Released = 2,
    Consumed = 3
}
