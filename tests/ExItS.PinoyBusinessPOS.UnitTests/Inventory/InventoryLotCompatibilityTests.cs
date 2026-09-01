using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryLotCompatibilityTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly CatalogProductId Coke = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PosBranchId Main = PosBranchId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-09-01T08:00:00Z");

    [Fact]
    public void H1_LOT_01_union_keeps_legacy_null_and_new_primary_lots()
    {
        var legacy = InventoryLot.Create(Org, Coke, new DateOnly(2026, 12, 31), 100m, T0, branchId: null);
        var neu = InventoryLot.Create(Org, Coke, new DateOnly(2027, 1, 31), 20m, T0, Main);

        var union = InventoryLotCompatibility.UnionByLotId([neu], [legacy]);

        Assert.Equal(2, union.Count);
        Assert.Equal(120m, union.Sum(l => l.QuantityOnHand));
    }

    [Fact]
    public void H1_LOT_02_union_does_not_double_count_same_lot_id()
    {
        var lot = InventoryLot.Create(Org, Coke, new DateOnly(2026, 12, 31), 100m, T0, Main);
        var union = InventoryLotCompatibility.UnionByLotId([lot], [lot]);
        Assert.Single(union);
        Assert.Equal(100m, union[0].QuantityOnHand);
    }

    [Fact]
    public void H1_LOT_02_org_level_query_semantics_exclude_branch_scoped_lots()
    {
        var branchLot = InventoryLot.Create(Org, Coke, new DateOnly(2027, 1, 31), 20m, T0, Main);
        var union = InventoryLotCompatibility.UnionByLotId([branchLot], [branchLot]);
        Assert.Single(union);
    }

    [Fact]
    public void Include_legacy_only_for_known_primary_target()
    {
        Assert.True(InventoryLotCompatibility.IncludeLegacyNullLots(Main.Value, Main));
        Assert.False(InventoryLotCompatibility.IncludeLegacyNullLots(Main.Value, PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))));
        Assert.False(InventoryLotCompatibility.IncludeLegacyNullLots(null, Main));
    }
}

public sealed class InventoryBranchBalanceReservationTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly CatalogProductId Product = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PosBranchId Branch = PosBranchId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-09-01T08:00:00Z");

    [Fact]
    public void Reserve_does_not_reduce_on_hand()
    {
        var balance = InventoryBranchBalance.Create(Org, Branch, Product, 100m, T0);
        balance.Reserve(10m, T0);
        Assert.Equal(100m, balance.OnHandQuantity);
        Assert.Equal(10m, balance.ReservedQuantity);
        Assert.Equal(90m, balance.AvailableQuantity);
    }

    [Fact]
    public void Consume_reduces_on_hand_once()
    {
        var balance = InventoryBranchBalance.Create(Org, Branch, Product, 100m, T0);
        balance.Reserve(10m, T0);
        balance.ConsumeReservation(10m, T0);
        Assert.Equal(90m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);
    }

    [Fact]
    public void H1_RES_05_available_is_on_hand_minus_branch_reserved()
    {
        Assert.Equal(10m, BranchStockResolver.ResolveAvailable(50m, 40m));
        Assert.Equal(40m, BranchStockResolver.ResolveAvailable(50m, 10m));
    }
}
