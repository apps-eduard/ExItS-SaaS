using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Common;

public sealed class PosDocumentNumbersTests
{
    [Fact]
    public void Format_uses_prefix_pads_to_three_digits_and_expands_past_999()
    {
        var day = new DateOnly(2026, 9, 22);
        Assert.Equal("SAL-260922-001", PosDocumentNumbers.Format(PosDocumentPrefixes.Sale, day, 1));
        Assert.Equal("SAL-260922-010", PosDocumentNumbers.Format(PosDocumentPrefixes.Sale, day, 10));
        Assert.Equal("SAL-260922-999", PosDocumentNumbers.Format(PosDocumentPrefixes.Sale, day, 999));
        Assert.Equal("SAL-260922-1000", PosDocumentNumbers.Format(PosDocumentPrefixes.Sale, day, 1000));

        Assert.Equal("PO-260922-001", PurchaseOrderNumbers.Format(day, 1));
        Assert.Equal("TR-260922-001", InventoryTransferNumbers.Format(day, 1));
        Assert.Equal("SR-260922-001", StockRequestNumbers.Format(day, 1));
        Assert.Equal("GRN-260922-001", GoodsReceiptNumbers.Format(day, 1));
        Assert.Equal("SAL-260922-001", SaleNumbers.Format(day, 1));
    }

    [Fact]
    public void Format_next_day_starts_independent_sequence_display()
    {
        Assert.Equal("SAL-260922-001", PosDocumentNumbers.Format(PosDocumentPrefixes.Sale, new DateOnly(2026, 9, 22), 1));
        Assert.Equal("SAL-260923-001", PosDocumentNumbers.Format(PosDocumentPrefixes.Sale, new DateOnly(2026, 9, 23), 1));
    }

    [Fact]
    public void FormatChild_keeps_root_and_appends_Rn()
    {
        Assert.Equal("TR-260922-001-R1", InventoryTransferNumbers.FormatReplacement("TR-260922-001", 1));
        Assert.Equal("TR-260922-001-R2", InventoryTransferNumbers.FormatReplacement("TR-260922-001", 2));
        Assert.Equal("TR-260922-001-R1", PosDocumentNumbers.Normalize(" tr-260922-001-r1 ", PosDocumentPrefixes.InventoryTransfer));
        Assert.Equal("TR-260922-001", PosDocumentNumbers.NormalizeRoot("TR-260922-001", PosDocumentPrefixes.InventoryTransfer));
        Assert.Throws<DomainException>(() =>
            PosDocumentNumbers.NormalizeRoot("TR-260922-001-R1", PosDocumentPrefixes.InventoryTransfer));
    }

    [Fact]
    public void FormatChild_keeps_historical_unprefixed_root()
    {
        Assert.Equal("260922-001-R1", PosDocumentNumbers.FormatChild("260922-001", 1));
    }

    [Fact]
    public void Historical_unprefixed_numbers_remain_readable()
    {
        Assert.Equal("260922-001", PosDocumentNumbers.Normalize(" 260922-001 "));
        Assert.Equal("260922-001", SaleNumbers.Normalize("260922-001"));
        Assert.Equal("260922-004-R1", PosDocumentNumbers.Normalize("260922-004-r1"));
    }

    [Fact]
    public void Invalid_and_unknown_prefixes_are_rejected()
    {
        foreach (var invalid in new[] { "", "SALE-260908-001", "ZZZ-260908-001", "260908-1", "260908", "26-001", "SAL-260908-001-X1" })
        {
            Assert.Throws<DomainException>(() => PosDocumentNumbers.Normalize(invalid));
        }

        Assert.Throws<DomainException>(() => SaleNumbers.Normalize("PO-260922-001"));
        Assert.Throws<DomainException>(() => PosDocumentNumbers.Format("ZZ", new DateOnly(2026, 9, 22), 1));
        Assert.Throws<DomainException>(() => PosDocumentNumbers.Format(PosDocumentPrefixes.Sale, new DateOnly(2026, 9, 22), 0));
        Assert.Throws<DomainException>(() => PosDocumentNumbers.FormatChild("TR-260922-001", 0));
    }

    [Fact]
    public void Wrappers_declare_expected_prefixes()
    {
        Assert.Equal("SAL", SaleNumbers.Prefix);
        Assert.Equal("PO", PurchaseOrderNumbers.Prefix);
        Assert.Equal("TR", InventoryTransferNumbers.Prefix);
        Assert.Equal("SR", StockRequestNumbers.Prefix);
        Assert.Equal("GRN", GoodsReceiptNumbers.Prefix);
    }
}
