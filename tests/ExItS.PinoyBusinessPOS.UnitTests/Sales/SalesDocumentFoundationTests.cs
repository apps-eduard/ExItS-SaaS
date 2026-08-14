using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

public sealed class SalesDocumentFoundationTests
{
    [Fact]
    public void Normal_sale_maps_to_transaction_summary()
    {
        var sale = Sale.Checkout(
            PosOrganizationId.From(Guid.NewGuid()),
            SaleNumbers.Format(new DateOnly(2026, 8, 14), 1),
            SalePaymentMethod.Cash,
            [new SaleLineDraft(
                CatalogProductId.New(),
                "Item",
                null,
                null,
                UnitOfMeasure.Piece,
                10m,
                1m)],
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero),
            amountTendered: 10m,
            cashierShiftId: CashierShiftId.New(),
            registerId: RegisterId.New());

        var dto = SaleQueryService.Map(sale);

        Assert.Equal(nameof(SalesDocumentKind.TransactionSummary), dto.DocumentKind);
    }

    [Fact]
    public void Tax_document_request_is_rejected()
    {
        var result = new RequestSalesDocument().Execute(SalesDocumentKind.TaxDocument);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.TaxDocumentIssuanceNotAvailable, result.ErrorCode);
    }
}
