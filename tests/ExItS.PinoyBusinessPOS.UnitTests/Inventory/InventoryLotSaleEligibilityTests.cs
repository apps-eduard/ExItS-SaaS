using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryLotSaleEligibilityTests
{
    private static readonly DateOnly Expiry = new(2026, 10, 10);

    [Theory]
    [InlineData(2026, 10, 9, 0, InventoryLotSaleEligibilityStatus.Sellable)]
    [InlineData(2026, 10, 10, 0, InventoryLotSaleEligibilityStatus.Sellable)]
    [InlineData(2026, 10, 11, 0, InventoryLotSaleEligibilityStatus.Expired)]
    [InlineData(2026, 10, 6, 3, InventoryLotSaleEligibilityStatus.Sellable)]
    [InlineData(2026, 10, 7, 3, InventoryLotSaleEligibilityStatus.SaleBlockedByExpiryPolicy)]
    [InlineData(2026, 10, 8, 3, InventoryLotSaleEligibilityStatus.SaleBlockedByExpiryPolicy)]
    [InlineData(2026, 10, 9, 3, InventoryLotSaleEligibilityStatus.SaleBlockedByExpiryPolicy)]
    [InlineData(2026, 10, 10, 3, InventoryLotSaleEligibilityStatus.SaleBlockedByExpiryPolicy)]
    [InlineData(2026, 10, 11, 3, InventoryLotSaleEligibilityStatus.Expired)]
    public void Evaluate_matches_formal_cutoff_semantics(
        int y,
        int m,
        int d,
        int stopDays,
        InventoryLotSaleEligibilityStatus expected)
    {
        var today = new DateOnly(y, m, d);
        Assert.Equal(expected, InventoryLotSaleEligibility.Evaluate(Expiry, today, stopDays));
    }

    [Fact]
    public void Zero_stop_days_never_policy_blocks_on_expiry_date()
    {
        Assert.True(InventoryLotSaleEligibility.IsSellable(Expiry, Expiry, 0));
        Assert.False(InventoryLotSaleEligibility.IsPolicyBlocked(Expiry, Expiry, 0));
    }

    [Fact]
    public void Past_expiry_is_absolute_even_when_stop_days_zero()
    {
        Assert.Equal(
            InventoryLotSaleEligibilityStatus.Expired,
            InventoryLotSaleEligibility.Evaluate(Expiry, Expiry.AddDays(1), 0));
    }
}

public sealed class ExpirySalePolicyResolverTests
{
    [Fact]
    public void Compose_precedence_branch_category_beats_branch_beats_org_category_beats_org()
    {
        // Org 0, Dairy org 2, Iloilo branch 3, Iloilo Fruits 0
        Assert.Equal(
            0,
            ExpirySalePolicyResolver.Compose(0, null, null, null).StopSellingDaysBeforeExpiry);
        Assert.Equal(
            ExpirySalePolicySource.OrganizationDefault,
            ExpirySalePolicyResolver.Compose(0, null, null, null).Source);

        Assert.Equal(
            2,
            ExpirySalePolicyResolver.Compose(0, 2, null, null).StopSellingDaysBeforeExpiry);
        Assert.Equal(
            ExpirySalePolicySource.OrganizationCategory,
            ExpirySalePolicyResolver.Compose(0, 2, null, null).Source);

        // Branch default beats org category
        Assert.Equal(
            3,
            ExpirySalePolicyResolver.Compose(0, 2, 3, null).StopSellingDaysBeforeExpiry);
        Assert.Equal(
            ExpirySalePolicySource.Branch,
            ExpirySalePolicyResolver.Compose(0, 2, 3, null).Source);

        Assert.Equal(
            0,
            ExpirySalePolicyResolver.Compose(0, 2, 3, 0).StopSellingDaysBeforeExpiry);
        Assert.Equal(
            ExpirySalePolicySource.BranchCategory,
            ExpirySalePolicyResolver.Compose(0, 2, 3, 0).Source);
    }
}

public sealed class InventoryLotFefoSalePolicyTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Utc = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Utc.UtcDateTime);

    [Fact]
    public void AllocateSellable_skips_policy_blocked_lots()
    {
        var lotA = InventoryLot.Create(Org, Product, new DateOnly(2026, 10, 7), 10m, Utc, lotNumber: "A");
        var lotB = InventoryLot.Create(Org, Product, new DateOnly(2026, 10, 20), 20m, Utc, lotNumber: "B");
        var lotC = InventoryLot.Create(Org, Product, new DateOnly(2026, 11, 1), 30m, Utc, lotNumber: "C");

        var allocations = InventoryLotFefo.AllocateSellable([lotA, lotB, lotC], 25m, Today, stopSellingDaysBeforeExpiry: 3);

        Assert.Equal(2, allocations.Count);
        Assert.Equal(lotB.Id, allocations[0].Lot.Id);
        Assert.Equal(20m, allocations[0].Quantity);
        Assert.Equal(lotC.Id, allocations[1].Lot.Id);
        Assert.Equal(5m, allocations[1].Quantity);
        Assert.Equal(10m, lotA.QuantityOnHand);
    }

    [Fact]
    public void ProjectSaleBuckets_partition_sums_to_physical()
    {
        var sellable = InventoryLot.Create(Org, Product, new DateOnly(2026, 11, 1), 70m, Utc);
        var blocked = InventoryLot.Create(Org, Product, new DateOnly(2026, 10, 7), 10m, Utc);
        var expired = InventoryLot.Create(Org, Product, new DateOnly(2026, 9, 1), 20m, Utc);

        var buckets = InventoryLotFefo.ProjectSaleBuckets([sellable, blocked, expired], Today, 3);
        Assert.Equal(70m, buckets.Sellable);
        Assert.Equal(10m, buckets.PolicyBlocked);
        Assert.Equal(20m, buckets.Expired);
        Assert.Equal(100m, buckets.Physical);
        Assert.Equal(buckets.Physical, buckets.Sellable + buckets.PolicyBlocked + buckets.Expired);
    }

    [Fact]
    public void Default_zero_allows_expiry_date_sale()
    {
        var lot = InventoryLot.Create(Org, Product, Today, 5m, Utc);
        Assert.Equal(5m, InventoryLotFefo.SellableQuantity([lot], Today, 0));
        var nextDay = Today.AddDays(1);
        Assert.Equal(0m, InventoryLotFefo.SellableQuantity([lot], nextDay, 0));
        Assert.Equal(5m, InventoryLotFefo.ExpiredQuantity([lot], nextDay));
    }
}
