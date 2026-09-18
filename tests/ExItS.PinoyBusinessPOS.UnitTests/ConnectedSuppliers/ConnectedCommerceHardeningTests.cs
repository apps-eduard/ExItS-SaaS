using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedCommerceHardeningTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Percentage_discounts_do_not_stack()
    {
        var result = ConnectedB2bPricingResolver.Resolve(new ConnectedB2bPricingResolver.PricingInputs(
            ProductOverridePrice: null,
            ProductCategoryId: null,
            CustomerCategoryDiscountPercent: 10m,
            CustomerDefaultDiscountPercent: 20m,
            OrganizationCategoryDiscountPercent: 30m,
            OrganizationDefaultDiscountPercent: 40m,
            SellingPrice: null,
            ExposureSupplierOrderPrice: 100m));

        Assert.Equal(90m, result.UnitPrice);
        Assert.Equal(10m, result.AppliedDiscountPercent);
        Assert.Equal(ConnectedCustomerPriceSource.CustomerCategory, result.Source);
    }

    [Fact]
    public void Pricing_precedence_customer_default_beats_org_levels()
    {
        var result = ConnectedB2bPricingResolver.Resolve(new ConnectedB2bPricingResolver.PricingInputs(
            ProductOverridePrice: null,
            ProductCategoryId: null,
            CustomerCategoryDiscountPercent: null,
            CustomerDefaultDiscountPercent: 15m,
            OrganizationCategoryDiscountPercent: 30m,
            OrganizationDefaultDiscountPercent: 40m,
            SellingPrice: null,
            ExposureSupplierOrderPrice: 100m));

        Assert.Equal(85m, result.UnitPrice);
        Assert.Equal(ConnectedCustomerPriceSource.CustomerDiscount, result.Source);
    }

    [Fact]
    public void Connected_commerce_percentage_settings_do_not_introduce_fixed_price_mode()
    {
        var result = ConnectedB2bPricingResolver.Resolve(new ConnectedB2bPricingResolver.PricingInputs(
            ProductOverridePrice: null,
            ProductCategoryId: null,
            CustomerCategoryDiscountPercent: null,
            CustomerDefaultDiscountPercent: null,
            OrganizationCategoryDiscountPercent: null,
            OrganizationDefaultDiscountPercent: 5m,
            SellingPrice: null,
            ExposureSupplierOrderPrice: 200m));

        Assert.Equal(190m, result.UnitPrice);
        Assert.Equal(ConnectedCustomerPriceSource.OrganizationDefault, result.Source);
    }

    [Fact]
    public void Legacy_product_fixed_override_still_wins_but_is_separate_from_percent_settings()
    {
        var result = ConnectedB2bPricingResolver.Resolve(new ConnectedB2bPricingResolver.PricingInputs(
            ProductOverridePrice: 42m,
            ProductCategoryId: null,
            CustomerCategoryDiscountPercent: 50m,
            CustomerDefaultDiscountPercent: 20m,
            OrganizationCategoryDiscountPercent: 10m,
            OrganizationDefaultDiscountPercent: 5m,
            SellingPrice: null,
            ExposureSupplierOrderPrice: 100m));

        Assert.Equal(42m, result.UnitPrice);
        Assert.Equal(0m, result.AppliedDiscountPercent);
        Assert.Equal(ConnectedCustomerPriceSource.ProductOverride, result.Source);
    }

    [Fact]
    public void PayBefore_gate_rejects_unpaid_and_accepts_settled_snapshot()
    {
        var (order, buyerPo) = CreateAcceptedOrder(
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            ConnectedPoPaymentTerm.Cash,
            total: 150m);

        var unpaid = ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                Array.Empty<GoodsReceipt>(),
                Array.Empty<SupplierPayable>()));
        Assert.False(unpaid.IsSatisfied);
        Assert.Equal(
            ConnectedSupplierDomainErrorCodes.PaymentRequiredBeforeFulfillment,
            ConnectedPoPayBeforeFulfillmentGate.Fail(unpaid).ErrorCode);

        buyerPo.RecordSettledPrepayment(150m, Now);
        var paid = ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                Array.Empty<GoodsReceipt>(),
                Array.Empty<SupplierPayable>()));
        Assert.True(paid.IsSatisfied);
    }

    [Fact]
    public void PayBefore_uncleared_check_is_blocked_cleared_succeeds()
    {
        var (order, buyerPo) = CreateAcceptedOrder(
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            ConnectedPoPaymentTerm.Check,
            total: 80m);

        buyerPo.RecordSettledPrepayment(80m, Now);
        Assert.True(ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                Array.Empty<GoodsReceipt>(),
                Array.Empty<SupplierPayable>())).IsSatisfied);

        var pendingReceipt = CreateCheckReceipt(buyerPo, UtangCheckClearingStatus.PendingClearing);
        var pendingPayable = CreatePayable(buyerPo, pendingReceipt, paid: 80m, SupplierPayablePaymentMethod.Check);
        Assert.False(ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                [pendingReceipt],
                [pendingPayable])).IsSatisfied);

        var clearedReceipt = CreateCheckReceipt(buyerPo, UtangCheckClearingStatus.Cleared);
        var clearedPayable = CreatePayable(buyerPo, clearedReceipt, paid: 80m, SupplierPayablePaymentMethod.Check);
        Assert.True(ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                [clearedReceipt],
                [clearedPayable])).IsSatisfied);
    }

    [Fact]
    public void PayOnDelivery_settlement_uses_good_received_value_only()
    {
        var productId = CatalogProductId.New();
        var po = PurchaseOrder.CreateDraft(
            PosOrganizationId.From(Guid.NewGuid()),
            SupplierId.From(Guid.NewGuid()),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new PurchaseOrderLineDraft(
                    productId,
                    10m,
                    12m,
                    NameSnapshot: "Item",
                    UomSnapshot: UnitOfMeasure.Piece)
            ],
            Now);
        po.Submit(
            "PO-20260918-000101",
            [
                new PurchaseOrderLineSnapshotInput(
                    productId,
                    "Item",
                    UnitOfMeasure.Piece,
                    10m,
                    12m)
            ],
            Guid.NewGuid(),
            Now.AddMinutes(1));
        po.ApplyReceiptLines(
            [
                new PurchaseOrderReceiveLineDraft(
                    productId,
                    4m,
                    SellingMode.PerItem,
                    DamagedQty: 1m,
                    RejectedQty: 5m,
                    ShortClosedQty: 0m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short)
            ],
            Now.AddMinutes(2));

        var snapshot = ConnectedPoShortCloseSettlement.Compute(
            po,
            Array.Empty<SupplierPayable>(),
            treatOutstandingAsCancelled: true);

        Assert.Equal(48m, snapshot.FinalAcceptedValue);
        Assert.Equal(4m, snapshot.GoodReceivedQty);
        Assert.True(snapshot.CancelledRemainingQty > 0m);
    }

    [Fact]
    public void SupplierCredit_timing_reserves_and_posts_accepted_good_value_only()
    {
        var (order, _) = CreateAcceptedOrder(
            ConnectedPoPaymentTiming.SupplierCredit,
            ConnectedPoPaymentTerm.Cash,
            total: 500m);

        Assert.True(ConnectedPoUtangCredit.UsesUtang(order));
        Assert.Equal(500m, ConnectedPoUtangCredit.ActiveReservationAmount(order));

        order.MarkFulfilled(Now.AddMinutes(1));
        order.PostUtangCreditFromReceipt(200m, Now.AddMinutes(2));
        Assert.Equal(200m, order.CreditPostedAmount);
        Assert.Equal(300m, ConnectedPoUtangCredit.ActiveReservationAmount(order));
    }

    [Fact]
    public void Customer_timing_cannot_enable_org_disabled_timing()
    {
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(supplier, Now);
        settings.ConfigurePaymentTiming(
            allowPayBeforeFulfillment: true,
            allowPayOnDeliveryOrReceipt: true,
            allowSupplierCredit: false,
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            Now);

        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            supplier,
            Now);
        relationship.Approve(Now);

        Assert.Throws<DomainException>(() =>
            relationship.ConfigurePaymentTimingOverrides(
                useOrganizationDefaults: false,
                allowPayBeforeFulfillment: true,
                allowPayOnDeliveryOrReceipt: true,
                allowSupplierCredit: true,
                customerDefaultPaymentTiming: ConnectedPoPaymentTiming.SupplierCredit,
                settings,
                Now));
    }

    [Fact]
    public void Customer_inherit_intersects_live_org_policy_without_copying_overrides()
    {
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(supplier, Now);
        settings.ConfigurePaymentTiming(
            allowPayBeforeFulfillment: true,
            allowPayOnDeliveryOrReceipt: true,
            allowSupplierCredit: true,
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            Now);

        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            supplier,
            Now);
        relationship.Approve(Now);
        relationship.ConfigurePaymentTimingOverrides(
            useOrganizationDefaults: true,
            allowPayBeforeFulfillment: false,
            allowPayOnDeliveryOrReceipt: false,
            allowSupplierCredit: false,
            customerDefaultPaymentTiming: ConnectedPoPaymentTiming.PayBeforeFulfillment,
            settings,
            Now);

        Assert.True(relationship.UseOrganizationPaymentTimingDefaults);
        var before = ConnectedPoPaymentTimingResolver.Resolve(settings, relationship);
        Assert.True(before.AllowSupplierCredit);
        Assert.Equal(ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt, before.DefaultPaymentTiming);

        settings.ConfigurePaymentTiming(
            allowPayBeforeFulfillment: true,
            allowPayOnDeliveryOrReceipt: false,
            allowSupplierCredit: false,
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            Now.AddMinutes(1));
        var after = ConnectedPoPaymentTimingResolver.Resolve(settings, relationship);
        Assert.False(after.AllowPayOnDeliveryOrReceipt);
        Assert.False(after.AllowSupplierCredit);
        Assert.Equal(ConnectedPoPaymentTiming.PayBeforeFulfillment, after.DefaultPaymentTiming);
    }

    [Fact]
    public void Proposal_hold_hours_normalize_to_safe_bounds()
    {
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(
            PosOrganizationId.From(Guid.NewGuid()),
            Now);
        settings.SetProposalReservationHoldHours(36, Now);
        Assert.Equal(36, settings.ProposalReservationHoldHours);

        Assert.Throws<DomainException>(() =>
            settings.SetProposalReservationHoldHours(0, Now));
        Assert.Throws<DomainException>(() =>
            settings.SetProposalReservationHoldHours(100, Now));
    }

    [Fact]
    public void Reservation_hold_service_reads_org_setting_source()
    {
        var path = Path.Combine(
            RepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Purchasing",
            "ConnectedPoInventoryReservationService.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("ProposalReservationHoldHours", source, StringComparison.Ordinal);
        Assert.Contains("utcNow.AddHours(holdHours)", source, StringComparison.Ordinal);
        Assert.Contains("MinProposalReservationHoldHours", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Existing_po_migration_backfill_avoids_blanket_PayBefore()
    {
        var path = Path.Combine(
            RepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Infrastructure",
            "Persistence",
            "Migrations",
            "20260918065634_AddConnectedCommerceOrgSettings.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("WHEN 2 THEN 2 ELSE 1 END", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "SET payment_timing = 0",
            source,
            StringComparison.Ordinal);
        Assert.Contains("confirmed_payment_timing = payment_timing", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirmed_payment_timing_snapshot_is_independent_of_later_org_defaults()
    {
        var (order, _) = CreateAcceptedOrder(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            total: 50m);

        Assert.Equal(ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt, order.EffectivePaymentTiming);
        Assert.Equal(ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt, order.ConfirmedPaymentTiming);
        Assert.Equal(50m, order.ConfirmedTotalAmount);
        Assert.Equal(50m, order.Lines[0].ConfirmedUnitPrice);
    }

    private static (ConnectedPurchaseOrder Order, PurchaseOrder BuyerPo) CreateAcceptedOrder(
        ConnectedPoPaymentTiming timing,
        ConnectedPoPaymentTerm term,
        decimal total)
    {
        var buyerOrg = PosOrganizationId.From(Guid.NewGuid());
        var supplierOrg = PosOrganizationId.From(Guid.NewGuid());
        var relationship = ConnectedSupplierRelationship.Request(buyerOrg, supplierOrg, Now);
        relationship.Approve(Now);

        var productId = CatalogProductId.From(Guid.NewGuid());
        var lines = new[]
        {
            ConnectedPurchaseOrderLine.Create(
                productId,
                "Item",
                "SKU",
                1m,
                total,
                "Piece")
        };

        var buyerPo = PurchaseOrder.CreateDraft(
            buyerOrg,
            SupplierId.From(Guid.NewGuid()),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new PurchaseOrderLineDraft(
                    productId,
                    1m,
                    total,
                    NameSnapshot: "Item",
                    UomSnapshot: UnitOfMeasure.Piece)
            ],
            Now,
            paymentTerm: term,
            paymentTiming: timing);
        buyerPo.Submit(
            "PO-20260918-000042",
            [
                new PurchaseOrderLineSnapshotInput(
                    productId,
                    "Item",
                    UnitOfMeasure.Piece,
                    1m,
                    total)
            ],
            Guid.NewGuid(),
            Now.AddMinutes(1));

        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            buyerPo.Id,
            buyerPo.PoNumber,
            buyerPo.OrderDate,
            notes: null,
            lines,
            Now,
            paymentTerm: term,
            paymentTiming: timing);
        order.Accept(Now);
        return (order, buyerPo);
    }

    private static GoodsReceipt CreateCheckReceipt(PurchaseOrder buyerPo, UtangCheckClearingStatus clearing)
    {
        var line = buyerPo.Lines[0];
        var grnId = GoodsReceiptId.New();
        var grnLine = GoodsReceiptLine.Rehydrate(
            GoodsReceiptLineId.New(),
            grnId,
            buyerPo.OrganizationId,
            line.Id,
            line.ProductId!,
            lineNumber: 1,
            nameSnapshot: line.NameSnapshot ?? "Item",
            uomSnapshot: line.UomSnapshot ?? UnitOfMeasure.Piece,
            quantityReceived: 1m,
            unitPurchaseCostSnapshot: line.UnitPurchaseCost,
            lineTotalSnapshot: line.UnitPurchaseCost,
            inventoryMovementId: null);

        return GoodsReceipt.Rehydrate(
            grnId,
            buyerPo.OrganizationId,
            buyerPo.Id,
            buyerPo.SupplierId,
            "GRN-1",
            DateOnly.FromDateTime(Now.UtcDateTime),
            deliveryReference: null,
            notes: null,
            receivedAtUtc: Now,
            receivedBy: Guid.NewGuid(),
            receivingBranchId: PosBranchId.From(Guid.NewGuid()),
            lines: [grnLine],
            settlement: new GoodsReceiptSettlement(
                null,
                "BDO",
                null,
                null,
                "CHK-1",
                DateOnly.FromDateTime(Now.UtcDateTime),
                null,
                clearing));
    }

    private static SupplierPayable CreatePayable(
        PurchaseOrder buyerPo,
        GoodsReceipt receipt,
        decimal paid,
        SupplierPayablePaymentMethod method) =>
        SupplierPayable.Create(
            buyerPo.OrganizationId,
            buyerPo.SupplierId,
            SupplierPayableSourceType.GoodsReceipt,
            receipt.Id.Value,
            originalAmount: paid,
            createdBy: Guid.NewGuid(),
            utcNow: Now,
            paidNow: paid,
            paymentMethodAtReceipt: method);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
