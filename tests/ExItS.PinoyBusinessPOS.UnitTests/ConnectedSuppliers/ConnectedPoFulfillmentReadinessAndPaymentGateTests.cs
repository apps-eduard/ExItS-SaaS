using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedPoFulfillmentReadinessAndPaymentGateTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 14, 0, 0, TimeSpan.Zero);
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Supplier = PosOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly Guid BranchId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ReceivingBranchId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Draft_can_store_fulfillment_even_when_delivery_not_ready()
    {
        var po = PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.From(Guid.NewGuid()),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [new PurchaseOrderLineDraft(CatalogProductId.New(), 1m, 10m, NameSnapshot: "A", UomSnapshot: UnitOfMeasure.Piece)],
            Now,
            fulfillmentMethod: "Delivery",
            intendedReceivingBranchId: ReceivingBranchId,
            paymentTiming: ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.Equal(PurchaseOrderStatus.Draft, po.Status);
        Assert.Equal("Delivery", po.FulfillmentMethod);
    }

    [Fact]
    public async Task Submit_blocks_delivery_when_org_offer_delivery_off()
    {
        var relationship = ActiveRelationship();
        var gate = new ConnectedPoFulfillmentReadinessGate(
            new FakeBranches(pickupReady: true, deliveryReady: true),
            new FakeOrgOffer(false));
        var po = DraftPo("Delivery");

        var result = await gate.EnsureForSubmitAsync(relationship, po, forBuyerMessage: true);
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.FulfillmentNotReady, result.ErrorCode);
        Assert.Contains("Delivery is not currently available", result.ErrorMessage);
    }

    [Fact]
    public async Task Submit_blocks_when_relationship_blocks_delivery()
    {
        var relationship = ActiveRelationship();
        relationship.SetCustomerDeliveryOverride(CustomerDeliveryOverride.Block, Now);
        var gate = new ConnectedPoFulfillmentReadinessGate(
            new FakeBranches(pickupReady: true, deliveryReady: true),
            new FakeOrgOffer(true));
        var po = DraftPo("Delivery");

        var result = await gate.EnsureForSubmitAsync(relationship, po, forBuyerMessage: true);
        Assert.False(result.IsSuccess);
        Assert.Contains("disabled for this business relationship", result.ErrorMessage);
    }

    [Fact]
    public async Task Pickup_submit_succeeds_when_delivery_unavailable()
    {
        var relationship = ActiveRelationship();
        var gate = new ConnectedPoFulfillmentReadinessGate(
            new FakeBranches(pickupReady: true, deliveryReady: false),
            new FakeOrgOffer(false));
        var po = DraftPo("Pickup");

        var result = await gate.EnsureForSubmitAsync(relationship, po, forBuyerMessage: true);
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public async Task Submit_blocks_when_receiving_branch_missing()
    {
        var relationship = ActiveRelationship();
        var gate = new ConnectedPoFulfillmentReadinessGate(
            new FakeBranches(pickupReady: true, deliveryReady: true),
            new FakeOrgOffer(true));
        var po = DraftPo("Pickup", includeReceivingBranch: false);

        var result = await gate.EnsureForSubmitAsync(relationship, po, forBuyerMessage: true);
        Assert.False(result.IsSuccess);
        Assert.Contains("Receiving branch setup is incomplete", result.ErrorMessage);
    }

    [Fact]
    public async Task Valid_delivery_submit_succeeds()
    {
        var relationship = ActiveRelationship();
        var gate = new ConnectedPoFulfillmentReadinessGate(
            new FakeBranches(pickupReady: false, deliveryReady: true),
            new FakeOrgOffer(true));
        var po = DraftPo("Delivery");

        var result = await gate.EnsureForSubmitAsync(relationship, po, forBuyerMessage: true);
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public async Task Later_lifecycle_revalidation_blocks_when_pickup_becomes_unready()
    {
        var relationship = ActiveRelationship();
        var (order, buyerPo) = AcceptedOrder("Pickup", ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);
        var gate = new ConnectedPoFulfillmentReadinessGate(
            new FakeBranches(pickupReady: false, deliveryReady: false),
            new FakeOrgOffer(false));

        var result = await gate.EnsureForLifecycleAsync(relationship, order, buyerPo, forBuyerMessage: false);
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.FulfillmentNotReady, result.ErrorCode);
    }

    [Fact]
    public void PayOnDelivery_unpaid_prepare_gate_is_satisfied()
    {
        var (order, buyerPo) = AcceptedOrder("Pickup", ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);
        var evaluation = ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                Array.Empty<GoodsReceipt>(),
                Array.Empty<SupplierPayable>()));
        Assert.True(evaluation.IsSatisfied);
        Assert.Null(evaluation.FailureMessage);
    }

    [Fact]
    public void PayBefore_unpaid_prepare_gate_returns_payment_required_message()
    {
        var (order, buyerPo) = AcceptedOrder("Pickup", ConnectedPoPaymentTiming.PayBeforeFulfillment);
        var evaluation = ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                Array.Empty<GoodsReceipt>(),
                Array.Empty<SupplierPayable>()));
        Assert.False(evaluation.IsSatisfied);
        Assert.Equal(
            "Payment is required before fulfillment can begin.",
            evaluation.FailureMessage);
        Assert.DoesNotContain("prepayment", evaluation.FailureMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PayBefore_settled_prepare_gate_succeeds()
    {
        var (order, buyerPo) = AcceptedOrder("Pickup", ConnectedPoPaymentTiming.PayBeforeFulfillment, total: 90m);
        buyerPo.RecordSettledPrepayment(90m, Now);
        Assert.True(ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                Array.Empty<GoodsReceipt>(),
                Array.Empty<SupplierPayable>())).IsSatisfied);
    }

    [Fact]
    public void SupplierCredit_prepare_gate_is_satisfied_without_prepayment()
    {
        var (order, buyerPo) = AcceptedOrder("Pickup", ConnectedPoPaymentTiming.SupplierCredit);
        Assert.True(ConnectedPoPayBeforeFulfillmentGate.Evaluate(
            new ConnectedPoPayBeforeFulfillmentGate.SettlementInputs(
                order,
                buyerPo,
                Array.Empty<GoodsReceipt>(),
                Array.Empty<SupplierPayable>())).IsSatisfied);
    }

    [Fact]
    public void Confirmed_payment_timing_stays_locked_after_accept()
    {
        var (order, _) = AcceptedOrder("Delivery", ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);
        Assert.Equal(ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt, order.ConfirmedPaymentTiming);
        Assert.Equal(ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt, order.EffectivePaymentTiming);
        Assert.Equal("Delivery", order.ConfirmedFulfillmentMethod);
        Assert.Equal("Delivery", order.EffectiveFulfillmentMethod);
    }

    [Fact]
    public void Cash_label_does_not_imply_pay_on_delivery_timing()
    {
        Assert.Equal("Cash", ConnectedPoPaymentTerms.ToUiLabel(ConnectedPoPaymentTerm.Cash));
        Assert.Equal(
            "Pay on delivery / receipt",
            ConnectedPoPaymentTerms.ToUiLabel(ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt));
        Assert.Equal(
            "Pay before fulfillment",
            ConnectedPoPaymentTerms.ToUiLabel(ConnectedPoPaymentTiming.PayBeforeFulfillment));
    }

    private static ConnectedSupplierRelationship ActiveRelationship()
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now);
        relationship.SetSupplierLocation(BranchId, "Main Branch", Now);
        return relationship;
    }

    private static PurchaseOrder DraftPo(string method, bool includeReceivingBranch = true)
    {
        return PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.From(Guid.NewGuid()),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [new PurchaseOrderLineDraft(CatalogProductId.New(), 2m, 25m, NameSnapshot: "Item", UomSnapshot: UnitOfMeasure.Piece)],
            Now,
            intendedReceivingBranchId: includeReceivingBranch ? ReceivingBranchId : null,
            paymentTiming: ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            fulfillmentMethod: method,
            supplierBranchId: BranchId,
            supplierBranchName: "Main Branch");
    }

    private static (ConnectedPurchaseOrder Order, PurchaseOrder BuyerPo) AcceptedOrder(
        string method,
        ConnectedPoPaymentTiming timing,
        decimal total = 50m)
    {
        var relationship = ActiveRelationship();
        var productId = CatalogProductId.New();
        var buyerPo = PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.From(Guid.NewGuid()),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [new PurchaseOrderLineDraft(productId, 1m, total, NameSnapshot: "Item", UomSnapshot: UnitOfMeasure.Piece)],
            Now,
            intendedReceivingBranchId: ReceivingBranchId,
            paymentTiming: timing,
            fulfillmentMethod: method,
            supplierBranchId: BranchId,
            supplierBranchName: "Main Branch");
        buyerPo.Submit(
            "PO-20260918-000201",
            [new PurchaseOrderLineSnapshotInput(productId, "Item", UnitOfMeasure.Piece, 1m, total)],
            Guid.NewGuid(),
            Now.AddMinutes(1));

        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            buyerPo.Id,
            buyerPo.PoNumber,
            buyerPo.OrderDate,
            null,
            [
                ConnectedPurchaseOrderLine.Create(
                    productId,
                    "Item",
                    null,
                    1m,
                    total,
                    UnitOfMeasure.Piece.ToString())
            ],
            Now.AddMinutes(2),
            paymentTerm: ConnectedPoPaymentTerm.Cash,
            paymentTiming: timing,
            fulfillmentMethod: method);
        order.Accept(Now.AddMinutes(3));
        return (order, buyerPo);
    }

    private sealed class FakeOrgOffer(bool offer) : IOrganizationFulfillmentSettingsRepository
    {
        public Task<OrganizationFulfillmentSettings?> GetAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            if (!offer)
            {
                return Task.FromResult<OrganizationFulfillmentSettings?>(null);
            }

            var settings = OrganizationFulfillmentSettings.CreateDefault(organizationId, Now);
            settings.SetOfferDelivery(true, Now);
            return Task.FromResult<OrganizationFulfillmentSettings?>(settings);
        }

        public Task AddAsync(OrganizationFulfillmentSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(OrganizationFulfillmentSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeBranches(bool pickupReady, bool deliveryReady) : ICustomerOrderBranchDirectory
    {
        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(
            Guid sellerOrganizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrderBranchSnapshot?>(new(
                BranchId,
                "Main Branch",
                CustomerOrderingEnabled: true,
                PickupEnabled: pickupReady,
                DeliveryEnabled: deliveryReady,
                CustomerOrderingOperational: true,
                PickupOperational: pickupReady,
                DeliveryOperational: deliveryReady,
                OnlineOrdersPaused: false,
                StoreStatusMessage: null,
                Latitude: null,
                Longitude: null,
                DeliveryPolicy: null,
                PickupReady: pickupReady,
                DeliveryReady: deliveryReady));

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
            Guid sellerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>(Array.Empty<CustomerOrderBranchSnapshot>());
    }
}
