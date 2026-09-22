using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PurchaseOrderReceiveDiscrepancyTests
{
    [Fact]
    public void Full_good_qty_requires_no_classification()
    {
        PurchaseOrderReceiveDiscrepancy.EnsureValid(
            5m,
            new PurchaseOrderReceiveLineDraft(CatalogProductId.New(), 5m),
            UnitOfMeasure.Kilogram,
            SellingMode.ByWeight);
    }

    [Fact]
    public void Short_receipt_requires_damaged_plus_not_delivered_to_equal_discrepancy()
    {
        var draft = new PurchaseOrderReceiveLineDraft(
            CatalogProductId.New(),
            ReceiveQty: 3m,
            DamagedQty: 0.5m,
            RejectedQty: 1.5m,
            DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Other);
        PurchaseOrderReceiveDiscrepancy.EnsureValid(5m, draft, UnitOfMeasure.Kilogram, SellingMode.ByWeight);

        Assert.Throws<DomainException>(() =>
            PurchaseOrderReceiveDiscrepancy.EnsureValid(
                5m,
                new PurchaseOrderReceiveLineDraft(
                    CatalogProductId.New(),
                    ReceiveQty: 3m,
                    DamagedQty: 1m,
                    RejectedQty: 0m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged),
                UnitOfMeasure.Kilogram,
                SellingMode.ByWeight));
    }

    [Fact]
    public void Cancel_remaining_must_short_close_full_discrepancy()
    {
        PurchaseOrderReceiveDiscrepancy.EnsureValid(
            5m,
            new PurchaseOrderReceiveLineDraft(
                CatalogProductId.New(),
                ReceiveQty: 3m,
                RejectedQty: 2m,
                ShortClosedQty: 2m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
            UnitOfMeasure.Kilogram,
            SellingMode.ByWeight);

        Assert.Throws<DomainException>(() =>
            PurchaseOrderReceiveDiscrepancy.EnsureValid(
                5m,
                new PurchaseOrderReceiveLineDraft(
                    CatalogProductId.New(),
                    ReceiveQty: 3m,
                    RejectedQty: 2m,
                    ShortClosedQty: 1m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
                UnitOfMeasure.Kilogram,
                SellingMode.ByWeight));
    }

    [Fact]
    public void Other_qty_must_equal_remaining_discrepancy_with_reason()
    {
        PurchaseOrderReceiveDiscrepancy.EnsureValid(
            5m,
            new PurchaseOrderReceiveLineDraft(
                CatalogProductId.New(),
                ReceiveQty: 3m,
                OtherQty: 2m,
                OtherReasonCode: ReceiveDiscrepancyOtherReason.WrongItem,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.WrongItem),
            UnitOfMeasure.Kilogram,
            SellingMode.ByWeight);

        Assert.Throws<DomainException>(() =>
            PurchaseOrderReceiveDiscrepancy.EnsureValid(
                5m,
                new PurchaseOrderReceiveLineDraft(
                    CatalogProductId.New(),
                    ReceiveQty: 3m,
                    OtherQty: 2m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Other),
                UnitOfMeasure.Kilogram,
                SellingMode.ByWeight));
    }

    [Fact]
    public void Remaining_action_is_derived_from_persisted_receipt_quantities()
    {
        Assert.Equal(
            PurchaseOrderReceiveDiscrepancy.RemainingActionCancelRemaining,
            PurchaseOrderReceiveDiscrepancy.ResolveRemainingAction(2m, 0.5m, 1.5m));
        Assert.Equal(
            PurchaseOrderReceiveDiscrepancy.RemainingActionDeliverLater,
            PurchaseOrderReceiveDiscrepancy.ResolveRemainingAction(0m, 1m, 1m));
        Assert.Null(PurchaseOrderReceiveDiscrepancy.ResolveRemainingAction(0m, 0m, 0m));
    }
}
