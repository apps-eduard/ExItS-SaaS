using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedPoLifecycleDomainTests
{
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Supplier = PosOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProductA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static ConnectedPurchaseOrder NewOrder()
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        var line = ConnectedPurchaseOrderLine.Create(CatalogProductId.From(ProductA), "Coke", null, 10m, 50m, "Piece");
        return ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-1042",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(2));
    }

    private static PurchaseOrder BuyerPo(int orderedQty = 20)
    {
        var po = PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.New(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), orderedQty, 10m)],
            Now);
        po.Submit(
            "PO-20260816-001042",
            [new PurchaseOrderLineSnapshotInput(
                CatalogProductId.From(ProductA),
                "Coke",
                UnitOfMeasure.Piece,
                orderedQty,
                10m)],
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Now.AddMinutes(1));
        return po;
    }

    [Fact]
    public void Decline_records_reason_and_note()
    {
        var order = NewOrder();
        order.Decline(Now.AddMinutes(3), ConnectedPoDeclineReason.OutOfStock, "No stock today");
        Assert.Equal(ConnectedPurchaseOrderStatus.Declined, order.Status);
        Assert.Equal(ConnectedPoDeclineReason.OutOfStock, order.DeclineReason);
        Assert.Equal("No stock today", order.DeclineNote);
        Assert.NotNull(order.DeclinedAtUtc);
    }

    [Fact]
    public void Prepare_and_fulfill_transitions_are_valid()
    {
        var order = NewOrder();
        order.Accept(Now.AddMinutes(3));
        order.StartPreparing(Now.AddMinutes(4));
        Assert.Equal(ConnectedPurchaseOrderStatus.Preparing, order.Status);
        order.MarkFulfilled(Now.AddMinutes(5));
        Assert.Equal(ConnectedPurchaseOrderStatus.Fulfilled, order.Status);
        Assert.True(order.CanBuyerReceive);
        Assert.False(order.CanBuyerWithdraw);
    }

    [Fact]
    public void Accept_then_direct_fulfill_is_allowed()
    {
        var order = NewOrder();
        order.Accept(Now.AddMinutes(3));
        order.MarkFulfilled(Now.AddMinutes(4));
        Assert.Equal(ConnectedPurchaseOrderStatus.Fulfilled, order.Status);
        Assert.NotNull(order.PreparingAtUtc);
    }

    [Fact]
    public void Withdraw_only_from_new()
    {
        var order = NewOrder();
        order.WithdrawByBuyer(Now.AddMinutes(3));
        Assert.Equal(ConnectedPurchaseOrderStatus.Withdrawn, order.Status);
        Assert.False(order.CanBuyerReceive);

        var accepted = NewOrder();
        accepted.Accept(Now.AddMinutes(3));
        var ex = Assert.Throws<DomainException>(() => accepted.WithdrawByBuyer(Now.AddMinutes(4)));
        Assert.Equal(ConnectedSupplierDomainErrorCodes.InvalidTransition, ex.ErrorCode);
    }

    [Fact]
    public void Invalid_fulfillment_transition_is_rejected()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() => order.StartPreparing(Now.AddMinutes(3)));
        order.Decline(Now.AddMinutes(3));
        Assert.Throws<DomainException>(() => order.MarkFulfilled(Now.AddMinutes(4)));
    }

    [Fact]
    public void Display_status_maps_buyer_and_supplier_views()
    {
        var order = NewOrder();
        var po = BuyerPo();
        Assert.Equal(ConnectedPoDisplayStatus.WaitingForSupplier, ConnectedPoDisplayStatus.ForBuyer(po, order));

        order.Accept(Now.AddMinutes(3));
        Assert.Equal(ConnectedPoDisplayStatus.SupplierAccepted, ConnectedPoDisplayStatus.ForBuyer(po, order));
        Assert.Equal("Accepted", ConnectedPoDisplayStatus.ForSupplier(order));

        order.StartPreparing(Now.AddMinutes(4));
        Assert.Equal(ConnectedPoDisplayStatus.Preparing, ConnectedPoDisplayStatus.ForBuyer(po, order));

        order.MarkFulfilled(Now.AddMinutes(5));
        Assert.Equal(ConnectedPoDisplayStatus.Ready, ConnectedPoDisplayStatus.ForBuyer(po, order));
    }

    [Fact]
    public void Status_transition_matrix_blocks_contradictory_races()
    {
        Assert.True(ConnectedPoDisplayStatus.IsValidConnectedStatusTransition(
            ConnectedPurchaseOrderStatus.New, ConnectedPurchaseOrderStatus.Accepted));
        Assert.True(ConnectedPoDisplayStatus.IsValidConnectedStatusTransition(
            ConnectedPurchaseOrderStatus.New, ConnectedPurchaseOrderStatus.Withdrawn));
        Assert.True(ConnectedPoDisplayStatus.IsValidConnectedStatusTransition(
            ConnectedPurchaseOrderStatus.New, ConnectedPurchaseOrderStatus.ChangesProposed));
        Assert.True(ConnectedPoDisplayStatus.IsValidConnectedStatusTransition(
            ConnectedPurchaseOrderStatus.ChangesProposed, ConnectedPurchaseOrderStatus.Accepted));
        Assert.False(ConnectedPoDisplayStatus.IsValidConnectedStatusTransition(
            ConnectedPurchaseOrderStatus.Accepted, ConnectedPurchaseOrderStatus.Withdrawn));
        Assert.False(ConnectedPoDisplayStatus.IsValidConnectedStatusTransition(
            ConnectedPurchaseOrderStatus.Withdrawn, ConnectedPurchaseOrderStatus.Accepted));
    }

    [Fact]
    public void Accept_unchanged_confirms_requested_quantities()
    {
        var order = NewOrder();
        order.Accept(Now.AddMinutes(3));
        Assert.Equal(ConnectedPurchaseOrderStatus.Accepted, order.Status);
        Assert.Equal(10m, order.Lines[0].Qty);
        Assert.Equal(10m, order.Lines[0].ConfirmedQty);
        Assert.Equal(order.TotalAmount, order.ConfirmedTotalAmount);
        Assert.False(order.HasProposedLineChanges);
    }

    [Fact]
    public void Supplier_can_reduce_qty_and_buyer_must_approve()
    {
        var order = NewOrder();
        order.ProposeLineChanges(
            [new ConnectedPoLineProposal(CatalogProductId.From(ProductA), 8m, Unavailable: false)],
            Now.AddMinutes(3));
        Assert.Equal(ConnectedPurchaseOrderStatus.ChangesProposed, order.Status);
        Assert.Equal(10m, order.Lines[0].Qty);
        Assert.Equal(8m, order.Lines[0].ProposedQty);
        Assert.Null(order.Lines[0].ConfirmedQty);
        Assert.Equal(400m, order.ProposedTotalAmount);
        Assert.Equal(500m, order.TotalAmount);

        order.AcceptProposedChanges(Now.AddMinutes(4));
        Assert.Equal(ConnectedPurchaseOrderStatus.Accepted, order.Status);
        Assert.Equal(8m, order.Lines[0].ConfirmedQty);
        Assert.Equal(10m, order.Lines[0].Qty);
    }

    [Fact]
    public void Supplier_can_mark_line_unavailable()
    {
        var order = TwoLineOrder();
        order.ProposeLineChanges(
            [
                new ConnectedPoLineProposal(CatalogProductId.From(ProductA), 10m, false),
                new ConnectedPoLineProposal(CatalogProductId.From(ProductB), 0m, true)
            ],
            Now.AddMinutes(3));
        Assert.Equal(ConnectedPoLineAvailability.Unavailable, order.Lines[1].Availability);
        Assert.Equal(0m, order.Lines[1].ProposedQty);
        Assert.Equal(5m, order.Lines[1].Qty);
    }

    [Fact]
    public void Supplier_cannot_increase_requested_qty_or_change_price()
    {
        var order = NewOrder();
        var ex = Assert.Throws<DomainException>(() => order.ProposeLineChanges(
            [new ConnectedPoLineProposal(CatalogProductId.From(ProductA), 11m, false)],
            Now.AddMinutes(3)));
        Assert.Equal(ConnectedSupplierDomainErrorCodes.InvalidOrder, ex.ErrorCode);
        Assert.Equal(10m, order.Lines[0].Qty);
        Assert.Equal(50m, order.Lines[0].UnitPriceSnapshot);
    }

    [Fact]
    public void All_unavailable_cannot_be_proposed_as_fulfillment()
    {
        var order = NewOrder();
        var ex = Assert.Throws<DomainException>(() => order.ProposeLineChanges(
            [new ConnectedPoLineProposal(CatalogProductId.From(ProductA), 0m, true)],
            Now.AddMinutes(3)));
        Assert.Equal(ConnectedSupplierDomainErrorCodes.InvalidOrder, ex.ErrorCode);
        Assert.Equal(ConnectedPurchaseOrderStatus.New, order.Status);
    }

    [Fact]
    public void Buyer_rejection_closes_proposed_changes()
    {
        var order = NewOrder();
        order.ProposeLineChanges(
            [new ConnectedPoLineProposal(CatalogProductId.From(ProductA), 8m, false)],
            Now.AddMinutes(3));
        order.RejectProposedChanges(Now.AddMinutes(4));
        Assert.Equal(ConnectedPurchaseOrderStatus.Withdrawn, order.Status);
        Assert.Equal(10m, order.Lines[0].Qty);
        Assert.False(order.CanBuyerWithdraw);
    }

    [Fact]
    public void Align_outstanding_short_closes_unconfirmed_qty_without_changing_ordered()
    {
        var po = BuyerPo(10);
        po.AlignOutstandingToConfirmedQuantities(
            new Dictionary<Guid, decimal> { [ProductA] = 8m },
            Now.AddMinutes(5));
        Assert.Equal(10m, po.Lines[0].OrderedQty);
        Assert.Equal(8m, po.Lines[0].OutstandingQty);
        Assert.Equal(2m, po.Lines[0].ClosedShortQty);
        Assert.Equal(0m, po.Lines[0].ReceivedQty);
        Assert.Equal(PurchaseOrderStatus.Ordered, po.Status);
    }

    [Fact]
    public void Payment_terms_parse_gcash_as_manual()
    {
        Assert.Equal(ConnectedPoPaymentTerm.Cash, ConnectedPoPaymentTerms.Parse("Cash"));
        Assert.Equal(ConnectedPoPaymentTerm.ManualGCash, ConnectedPoPaymentTerms.Parse("GCash"));
        Assert.Equal(ConnectedPoPaymentTerm.ManualGCash, ConnectedPoPaymentTerms.Parse("ManualGCash"));
        Assert.Equal(ConnectedPoPaymentTerm.Utang, ConnectedPoPaymentTerms.Parse("Utang"));
        Assert.Equal("GCash", ConnectedPoPaymentTerms.ToUiLabel(ConnectedPoPaymentTerm.ManualGCash));
        Assert.Equal("ManualGCash", ConnectedPoPaymentTerms.ToApi(ConnectedPoPaymentTerm.ManualGCash));
    }

    [Fact]
    public void Display_status_maps_proposed_changes_for_buyer()
    {
        var order = NewOrder();
        var po = BuyerPo();
        order.ProposeLineChanges(
            [new ConnectedPoLineProposal(CatalogProductId.From(ProductA), 8m, false)],
            Now.AddMinutes(3));
        Assert.Equal(ConnectedPoDisplayStatus.ChangesNeedApproval, ConnectedPoDisplayStatus.ForBuyer(po, order));
        Assert.Equal("ChangesProposed", ConnectedPoDisplayStatus.ForSupplier(order));
    }

    private static readonly Guid ProductB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static ConnectedPurchaseOrder TwoLineOrder()
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        return ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-1043",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [
                ConnectedPurchaseOrderLine.Create(CatalogProductId.From(ProductA), "Apple", null, 10m, 50m, "kg"),
                ConnectedPurchaseOrderLine.Create(CatalogProductId.From(ProductB), "Banana", null, 5m, 40m, "kg")
            ],
            Now.AddMinutes(2),
            paymentTerm: ConnectedPoPaymentTerm.Utang);
    }

    [Fact]
    public void Partial_receipt_with_short_close_sets_received_with_issues()
    {
        var po = BuyerPo(20);
        po.ApplyReceiptLines(
            [new PurchaseOrderReceiveLineDraft(
                CatalogProductId.From(ProductA),
                ReceiveQty: 18m,
                DamagedQty: 0m,
                ShortClosedQty: 2m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short)],
            Now.AddMinutes(10));

        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        Assert.True(po.HasReceivingIssues);
        Assert.Equal(0m, po.Lines[0].OutstandingQty);
        Assert.Equal(2m, po.Lines[0].ClosedShortQty);
        var connected = NewOrder();
        connected.Accept(Now.AddMinutes(3));
        Assert.Equal(
            ConnectedPoDisplayStatus.ReceivedWithIssues,
            ConnectedPoDisplayStatus.ForBuyer(po, connected));
    }

    [Fact]
    public void Keep_outstanding_produces_partially_received()
    {
        var po = BuyerPo(20);
        po.ApplyReceiptLines(
            [new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 18m)],
            Now.AddMinutes(10));
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, po.Status);
        Assert.Equal(2m, po.Lines[0].OutstandingQty);
        Assert.False(po.HasReceivingIssues);
    }

    [Fact]
    public void Damaged_and_rejected_do_not_count_as_received_qty()
    {
        var po = BuyerPo(10);
        po.ApplyReceiptLines(
            [new PurchaseOrderReceiveLineDraft(
                CatalogProductId.From(ProductA),
                ReceiveQty: 9m,
                DamagedQty: 1m,
                RejectedQty: 0m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged)],
            Now.AddMinutes(10));

        Assert.Equal(9m, po.Lines[0].ReceivedQty);
        Assert.Equal(1m, po.Lines[0].OutstandingQty);
    }

    [Fact]
    public void Over_receipt_is_rejected()
    {
        var po = BuyerPo(10);
        Assert.Throws<DomainException>(() => po.ApplyReceiptLines(
            [new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 11m)],
            Now.AddMinutes(10)));
    }
}
