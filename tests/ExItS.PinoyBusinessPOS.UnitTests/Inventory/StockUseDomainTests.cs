using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class StockUseDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId ProductA = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Numbers_format_and_normalize()
    {
        var date = new DateOnly(2026, 8, 29);
        Assert.Equal("SU-20260829-000001", StockUseNumbers.Format(date, 1));
        Assert.Equal("SU-20260829-000001", StockUseNumbers.Normalize(" su-20260829-000001 "));
    }

    [Fact]
    public void Create_and_void_posted_stock_use()
    {
        var stockUse = StockUse.Create(
            Org,
            "SU-20260829-000001",
            StockUseReason.InternalOperations,
            [Draft(ProductA, "Coke", 2m)],
            Actor,
            Now,
            notes: "Break room");

        Assert.Equal(StockUseStatus.Posted, stockUse.Status);
        Assert.Equal(StockUseReason.InternalOperations, stockUse.Reason);
        Assert.Equal("Break room", stockUse.Notes);
        Assert.Single(stockUse.Lines);
        Assert.Equal(2m, stockUse.Lines[0].BaseQuantity);

        stockUse.Void(Now.AddMinutes(1), Actor);
        Assert.Equal(StockUseStatus.Voided, stockUse.Status);
        Assert.Equal(Actor, stockUse.VoidedByUserId);
        Assert.NotNull(stockUse.VoidedAtUtc);

        var again = Assert.Throws<DomainException>(() => stockUse.Void(Now.AddMinutes(2), Actor));
        Assert.Equal(DomainErrorCodes.InvalidStockUseStatusTransition, again.ErrorCode);
    }

    [Fact]
    public void Create_rejects_empty_lines_and_invalid_qty()
    {
        var empty = Assert.Throws<DomainException>(() =>
            StockUse.Create(Org, "SU-20260829-000002", StockUseReason.StaffUse, [], Actor, Now));
        Assert.Equal(DomainErrorCodes.StockUseRequiresLines, empty.ErrorCode);

        var zero = Assert.Throws<DomainException>(() =>
            StockUse.Create(
                Org,
                "SU-20260829-000003",
                StockUseReason.Other,
                [Draft(ProductA, "Coke", 0m)],
                Actor,
                Now));
        Assert.Equal(DomainErrorCodes.InvalidStockUseQuantity, zero.ErrorCode);
    }

    [Fact]
    public void Reasons_parse_known_codes()
    {
        Assert.True(StockUseReasons.TryParse("InternalOperations", out var reason));
        Assert.Equal(StockUseReason.InternalOperations, reason);
        Assert.False(StockUseReasons.TryParse("Waste", out _));
    }

    private static StockUseLineDraft Draft(CatalogProductId productId, string name, decimal qty) =>
        new(productId, qty, 1m, name, "Piece");
}
