using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class WasteLossDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId ProductA = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Numbers_format_and_normalize()
    {
        var date = new DateOnly(2026, 8, 29);
        Assert.Equal("WL-20260829-000001", WasteLossNumbers.Format(date, 1));
        Assert.Equal("WL-20260829-000001", WasteLossNumbers.Normalize(" wl-20260829-000001 "));
    }

    [Fact]
    public void Create_and_void_posted_waste_loss()
    {
        var wasteLoss = WasteLoss.Create(
            Org,
            "WL-20260829-000001",
            WasteLossReason.Damaged,
            [Draft(ProductA, "Coke", 2m, unitCost: 5m)],
            Actor,
            Now,
            notes: "Broken crate");

        Assert.Equal(WasteLossStatus.Posted, wasteLoss.Status);
        Assert.Equal(WasteLossReason.Damaged, wasteLoss.Reason);
        Assert.Equal(ProductionCostStatus.Complete, wasteLoss.CostStatus);
        Assert.Equal(10m, wasteLoss.TotalCostSnapshot);
        Assert.Single(wasteLoss.Lines);

        wasteLoss.Void(Now.AddMinutes(1), Actor);
        Assert.Equal(WasteLossStatus.Voided, wasteLoss.Status);
        Assert.Equal(Actor, wasteLoss.VoidedByUserId);

        var again = Assert.Throws<DomainException>(() => wasteLoss.Void(Now.AddMinutes(2), Actor));
        Assert.Equal(DomainErrorCodes.InvalidWasteLossStatusTransition, again.ErrorCode);
    }

    [Fact]
    public void Other_reason_requires_notes()
    {
        var ex = Assert.Throws<DomainException>(() =>
            WasteLoss.Create(
                Org,
                "WL-20260829-000002",
                WasteLossReason.Other,
                [Draft(ProductA, "Coke", 1m)],
                Actor,
                Now));
        Assert.Equal(DomainErrorCodes.WasteLossOtherRequiresNotes, ex.ErrorCode);
    }

    [Fact]
    public void Cost_status_partial_and_unavailable()
    {
        var partial = WasteLoss.Create(
            Org,
            "WL-20260829-000003",
            WasteLossReason.Spoiled,
            [
                Draft(ProductA, "Coke", 1m, unitCost: 5m),
                Draft(CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")), "Sprite", 1m)
            ],
            Actor,
            Now);
        Assert.Equal(ProductionCostStatus.Partial, partial.CostStatus);

        var unavailable = WasteLoss.Create(
            Org,
            "WL-20260829-000004",
            WasteLossReason.Expired,
            [Draft(ProductA, "Coke", 1m)],
            Actor,
            Now);
        Assert.Equal(ProductionCostStatus.Unavailable, unavailable.CostStatus);
        Assert.Null(unavailable.TotalCostSnapshot);
    }

    [Fact]
    public void Create_rejects_empty_lines_and_invalid_qty()
    {
        var empty = Assert.Throws<DomainException>(() =>
            WasteLoss.Create(Org, "WL-20260829-000005", WasteLossReason.Broken, [], Actor, Now));
        Assert.Equal(DomainErrorCodes.WasteLossRequiresLines, empty.ErrorCode);

        var zero = Assert.Throws<DomainException>(() =>
            WasteLoss.Create(
                Org,
                "WL-20260829-000006",
                WasteLossReason.Spillage,
                [Draft(ProductA, "Coke", 0m)],
                Actor,
                Now));
        Assert.Equal(DomainErrorCodes.InvalidWasteLossQuantity, zero.ErrorCode);
    }

    [Fact]
    public void Reasons_parse_known_codes()
    {
        Assert.True(WasteLossReasons.TryParse("MissingOrShrinkage", out var reason));
        Assert.Equal(WasteLossReason.MissingOrShrinkage, reason);
        Assert.False(WasteLossReasons.TryParse("InternalOperations", out _));
    }

    private static WasteLossLineDraft Draft(
        CatalogProductId productId,
        string name,
        decimal qty,
        decimal? unitCost = null) =>
        new(productId, qty, 1m, name, "Piece", UnitCostSnapshot: unitCost);
}
