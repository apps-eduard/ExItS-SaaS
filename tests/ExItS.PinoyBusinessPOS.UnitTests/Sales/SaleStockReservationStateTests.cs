using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

public sealed class SaleStockReservationStateTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.NewGuid());
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    private static Sale ElectronicSale() =>
        Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 8, 16), 1),
            SalePaymentMethod.Card,
            [new SaleLineDraft(CatalogProductId.New(), "Item", "SKU", null, UnitOfMeasure.Piece, 10m, 1m)],
            Actor,
            Now,
            cashierShiftId: Shift,
            registerId: Register);

    [Fact]
    public void Checkout_electronic_starts_with_none()
    {
        var sale = ElectronicSale();
        Assert.Equal(SaleStatus.AwaitingPayment, sale.Status);
        Assert.Equal(SaleStockReservationState.None, sale.StockReservationState);
    }

    [Fact]
    public void Reserve_release_consume_transitions_and_idempotency()
    {
        var sale = ElectronicSale();
        sale.MarkStockReserved(Now);
        Assert.Equal(SaleStockReservationState.Reserved, sale.StockReservationState);
        sale.MarkStockReserved(Now.AddSeconds(1));
        Assert.Equal(SaleStockReservationState.Reserved, sale.StockReservationState);

        sale.MarkStockReleased(Now.AddSeconds(2));
        Assert.Equal(SaleStockReservationState.Released, sale.StockReservationState);
        sale.MarkStockReleased(Now.AddSeconds(3));
        Assert.Equal(SaleStockReservationState.Released, sale.StockReservationState);

        sale.MarkStockReserved(Now.AddSeconds(4));
        Assert.Equal(SaleStockReservationState.Reserved, sale.StockReservationState);

        sale.MarkStockConsumed(Now.AddSeconds(5));
        Assert.Equal(SaleStockReservationState.Consumed, sale.StockReservationState);
        sale.MarkStockConsumed(Now.AddSeconds(6));
        Assert.Equal(SaleStockReservationState.Consumed, sale.StockReservationState);
    }

    [Fact]
    public void Invalid_transitions_throw()
    {
        var sale = ElectronicSale();
        var release = Assert.Throws<DomainException>(() => sale.MarkStockReleased(Now));
        Assert.Equal(DomainErrorCodes.InvalidSaleStockReservation, release.ErrorCode);

        sale.MarkStockReserved(Now);
        sale.MarkStockConsumed(Now.AddSeconds(1));
        var rereserve = Assert.Throws<DomainException>(() => sale.MarkStockReserved(Now.AddSeconds(2)));
        Assert.Equal(DomainErrorCodes.InvalidSaleStockReservation, rereserve.ErrorCode);
    }

    [Fact]
    public void Rehydrate_preserves_reservation_state()
    {
        var sale = ElectronicSale();
        sale.MarkStockReserved(Now);
        var rehydrated = Sale.Rehydrate(
            sale.Id,
            sale.OrganizationId,
            sale.SaleNumber,
            sale.Status,
            sale.PaymentMethod,
            sale.Subtotal,
            sale.Total,
            sale.TaxAmount,
            sale.AmountTendered,
            sale.ChangeAmount,
            sale.GCashReference,
            sale.RecordedAtUtc,
            sale.RecordedBy,
            sale.VoidedAtUtc,
            sale.VoidedBy,
            sale.VoidReason,
            sale.UpdatedAtUtc,
            sale.Lines,
            stockReservationState: SaleStockReservationState.Reserved);

        Assert.Equal(SaleStockReservationState.Reserved, rehydrated.StockReservationState);
    }
}
