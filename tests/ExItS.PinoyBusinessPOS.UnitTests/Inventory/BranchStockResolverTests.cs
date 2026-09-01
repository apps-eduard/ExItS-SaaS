using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class BranchStockResolverTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly CatalogProductId Coke = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PosBranchId Main = PosBranchId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosBranchId BranchB = PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-18T08:00:00Z");

    [Fact]
    public void New_branch_does_not_inherit_unallocated_primary_stock()
    {
        var balances = new List<InventoryBranchBalance>();
        Assert.Equal(100m, BranchStockResolver.ResolveOnHand(Main, Main.Value, 100m, balances, Coke));
        Assert.Equal(0m, BranchStockResolver.ResolveOnHand(BranchB, Main.Value, 100m, balances, Coke));
    }

    [Fact]
    public void Transfer_semantics_keep_main_and_branch_isolated()
    {
        var balances = new List<InventoryBranchBalance>
        {
            InventoryBranchBalance.Create(Org, Main, Coke, 70m, T0),
            InventoryBranchBalance.Create(Org, BranchB, Coke, 30m, T0)
        };

        Assert.Equal(70m, BranchStockResolver.ResolveOnHand(Main, Main.Value, 100m, balances, Coke));
        Assert.Equal(30m, BranchStockResolver.ResolveOnHand(BranchB, Main.Value, 100m, balances, Coke));
        Assert.Equal(30m, BranchStockResolver.ResolveAvailable(30m, 0m));
    }

    [Fact]
    public void Missing_non_primary_row_never_uses_org_total_as_branch_availability()
    {
        var main = InventoryBranchBalance.Create(Org, Main, Coke, 100m, T0);
        var available = BranchStockResolver.ResolveAvailable(
            BranchStockResolver.ResolveOnHand(BranchB, Main.Value, 100m, [main], Coke),
            branchReserved: 0m);
        Assert.Equal(0m, available);
    }

    [Fact]
    public void H1_WRITE_PRIMARY_ensure_throws_when_missing_and_primary_unknown()
    {
        var ex = Assert.Throws<DomainException>(() =>
            BranchStockResolver.EnsureBalance(Org, Main, Coke, 100m, primaryBranchId: null, [], T0));
        Assert.Equal(DomainErrorCodes.InventoryPrimaryUnavailable, ex.ErrorCode);
    }

    [Fact]
    public void EnsureBalance_seeds_non_primary_at_zero()
    {
        var balances = new List<InventoryBranchBalance>();
        var created = BranchStockResolver.EnsureBalance(Org, BranchB, Coke, 100m, Main.Value, balances, T0);
        Assert.Equal(0m, created.OnHandQuantity);
        Assert.Same(created, balances[0]);
    }

    [Fact]
    public void H1_PRIMARY_07_unknown_primary_never_assigns_unallocated_to_secondary()
    {
        Assert.Equal(0m, BranchStockResolver.ResolveOnHand(BranchB, primaryBranchId: null, 100m, [], Coke));
    }

    [Fact]
    public void H1_PRIMARY_08_unknown_primary_still_returns_explicit_secondary_balance()
    {
        var balances = new List<InventoryBranchBalance>
        {
            InventoryBranchBalance.Create(Org, BranchB, Coke, 25m, T0)
        };

        Assert.Equal(25m, BranchStockResolver.ResolveOnHand(BranchB, primaryBranchId: null, 125m, balances, Coke));
    }

    [Fact]
    public void H1_PRIMARY_01_main_implicit_legacy_stock_on_known_primary()
    {
        Assert.Equal(100m, BranchStockResolver.ResolveOnHand(Main, Main.Value, 100m, [], Coke));
    }

    [Fact]
    public void H1_PRIMARY_02_remote_only_actor_sees_zero_without_explicit_balance()
    {
        Assert.Equal(0m, BranchStockResolver.ResolveOnHand(BranchB, Main.Value, 100m, [], Coke));
    }

    [Fact]
    public void H1_PRIMARY_04_partial_legacy_main_implicit_when_remote_explicit()
    {
        var balances = new List<InventoryBranchBalance>
        {
            InventoryBranchBalance.Create(Org, BranchB, Coke, 25m, T0)
        };

        Assert.Equal(100m, BranchStockResolver.ResolveOnHand(Main, Main.Value, 125m, balances, Coke));
    }

    [Fact]
    public void H1_PRIMARY_05_remote_explicit_balance()
    {
        var balances = new List<InventoryBranchBalance>
        {
            InventoryBranchBalance.Create(Org, BranchB, Coke, 25m, T0)
        };

        Assert.Equal(25m, BranchStockResolver.ResolveOnHand(BranchB, Main.Value, 125m, balances, Coke));
    }
}
