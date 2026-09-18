using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Organization-owned purchase order. Draft lines are editable; submit freezes product snapshots
/// and allocates a PO number. Receiving is via immutable goods receipts only.
/// </summary>
public sealed class PurchaseOrder
{
    public const int SupplierReferenceMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int RemainingClosedReasonMaxLength = 512;
    public const int SellerSettlementRemarksMaxLength = 512;
    public const int MaxLineCount = 200;

    private readonly List<PurchaseOrderLine> _lines;

    public PurchaseOrderId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string? PoNumber { get; private set; }
    public SupplierId SupplierId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public string? SupplierReference { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? OrderedAtUtc { get; private set; }
    public Guid? OrderedBy { get; private set; }
    /// <summary>Authoritative cancel time. Never inferred from <see cref="UpdatedAtUtc"/>.</summary>
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    /// <summary>Actor who cancelled the local PO (distinct from connected withdraw metadata).</summary>
    public Guid? CancelledByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    /// <summary>Connected-PO settlement term. Cash default. Not proof of payment.</summary>
    public ConnectedPoPaymentTerm PaymentTerm { get; private set; }
    /// <summary>Connected-PO payment timing. Defaults to pay-before. Locked on connected confirm.</summary>
    public ConnectedPoPaymentTiming PaymentTiming { get; private set; }
    /// <summary>
    /// Historical supplier source branch (Platform branch id) snapshotted at PO create/update-draft.
    /// Null for manual suppliers and legacy rows. Immutable after Ordered.
    /// </summary>
    public Guid? SupplierBranchId { get; private set; }
    /// <summary>Display name for <see cref="SupplierBranchId"/> at snapshot time.</summary>
    public string? SupplierBranchNameSnapshot { get; private set; }
    /// <summary>
    /// Buyer branch expected to receive goods for this PO. When set, goods receipts must use the same branch.
    /// Null preserves legacy / connected-supplier behavior (receive at acting branch).
    /// </summary>
    public Guid? IntendedReceivingBranchId { get; private set; }
    /// <summary>When remaining outstanding was explicitly short-closed (seller Close remaining).</summary>
    public DateTimeOffset? RemainingClosedAtUtc { get; private set; }
    public Guid? RemainingClosedByUserId { get; private set; }
    public string? RemainingClosedReason { get; private set; }
    /// <summary>Good-received value snapshot at short-close (authoritative settlement base).</summary>
    public decimal? FinalAcceptedValue { get; private set; }
    /// <summary>Cancelled remaining value snapshot at short-close (not charged).</summary>
    public decimal? CancelledRemainingValue { get; private set; }
    /// <summary>Explicit refund-due when amount paid exceeds final accepted value. Never silently reduces payment history.</summary>
    public decimal RefundDueAmount { get; private set; }
    /// <summary>Total paid across receipt payables at short-close time.</summary>
    public decimal? AmountPaidSnapshot { get; private set; }
    /// <summary>
    /// Commercial settlement state. Independent of <see cref="Status"/>: goods may be Received
    /// while pay-on-delivery/receipt settlement is still outstanding.
    /// </summary>
    public ConnectedPoFinancialSettlementStatus FinancialSettlementStatus { get; private set; }
    /// <summary>Seller remarks captured when settlement was confirmed.</summary>
    public string? SellerSettlementRemarks { get; private set; }
    public DateTimeOffset? FinanciallySettledAtUtc { get; private set; }
    /// <summary>Actor who confirmed settlement. Null when settlement required no payment.</summary>
    public Guid? FinanciallySettledBy { get; private set; }

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines;

    private PurchaseOrder(
        PurchaseOrderId id,
        PosOrganizationId organizationId,
        string? poNumber,
        SupplierId supplierId,
        PurchaseOrderStatus status,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        string? supplierReference,
        string? notes,
        DateTimeOffset? orderedAtUtc,
        Guid? orderedBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        List<PurchaseOrderLine> lines,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash,
        ConnectedPoPaymentTiming paymentTiming = ConnectedPoPaymentTiming.PayBeforeFulfillment,
        Guid? supplierBranchId = null,
        string? supplierBranchNameSnapshot = null,
        Guid? intendedReceivingBranchId = null,
        DateTimeOffset? cancelledAtUtc = null,
        Guid? cancelledByUserId = null,
        DateTimeOffset? remainingClosedAtUtc = null,
        Guid? remainingClosedByUserId = null,
        string? remainingClosedReason = null,
        decimal? finalAcceptedValue = null,
        decimal? cancelledRemainingValue = null,
        decimal refundDueAmount = 0m,
        decimal? amountPaidSnapshot = null,
        ConnectedPoFinancialSettlementStatus financialSettlementStatus =
            ConnectedPoFinancialSettlementStatus.NotRequired,
        string? sellerSettlementRemarks = null,
        DateTimeOffset? financiallySettledAtUtc = null,
        Guid? financiallySettledBy = null)
    {
        Id = id;
        OrganizationId = organizationId;
        PoNumber = poNumber;
        SupplierId = supplierId;
        Status = status;
        OrderDate = orderDate;
        ExpectedDeliveryDate = expectedDeliveryDate;
        SupplierReference = supplierReference;
        Notes = notes;
        OrderedAtUtc = orderedAtUtc;
        OrderedBy = orderedBy;
        CancelledAtUtc = cancelledAtUtc;
        CancelledByUserId = cancelledByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        PaymentTerm = paymentTerm;
        PaymentTiming = paymentTiming;
        SupplierBranchId = NormalizeBranchId(supplierBranchId);
        SupplierBranchNameSnapshot = NormalizeBranchName(supplierBranchNameSnapshot);
        IntendedReceivingBranchId = NormalizeBranchId(intendedReceivingBranchId);
        RemainingClosedAtUtc = remainingClosedAtUtc;
        RemainingClosedByUserId = remainingClosedByUserId;
        RemainingClosedReason = remainingClosedReason;
        FinalAcceptedValue = finalAcceptedValue;
        CancelledRemainingValue = cancelledRemainingValue;
        RefundDueAmount = refundDueAmount < 0m ? 0m : SaleMoney.RoundMoney(refundDueAmount);
        AmountPaidSnapshot = amountPaidSnapshot is null
            ? null
            : SaleMoney.RoundMoney(amountPaidSnapshot.Value);
        FinancialSettlementStatus = financialSettlementStatus;
        SellerSettlementRemarks = NormalizeSellerSettlementRemarks(sellerSettlementRemarks);
        FinanciallySettledAtUtc = financiallySettledAtUtc;
        FinanciallySettledBy = financiallySettledBy == Guid.Empty ? null : financiallySettledBy;
        _lines = lines;
    }

    /// <summary>
    /// Records settled prepayment toward PayBefore fulfillment (does not short-close the PO).
    /// Cumulative; never decreases. Used when settlement is confirmed before goods receipt.
    /// </summary>
    public void RecordSettledPrepayment(decimal settledAmount, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (RemainingClosedAtUtc is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Settled prepayment cannot be recorded after remaining quantity was closed.");
        }

        var rounded = SaleMoney.RoundMoney(settledAmount);
        if (rounded < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Settled prepayment cannot be negative.");
        }

        var current = AmountPaidSnapshot ?? 0m;
        AmountPaidSnapshot = SaleMoney.RoundMoney(Math.Max(current, rounded));
        UpdatedAtUtc = utcNow;
    }

    public static PurchaseOrder CreateDraft(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        DateOnly orderDate,
        IReadOnlyList<PurchaseOrderLineDraft> lines,
        DateTimeOffset utcNow,
        DateOnly? expectedDeliveryDate = null,
        string? supplierReference = null,
        string? notes = null,
        PurchaseOrderId? id = null,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash,
        Guid? createdBy = null,
        Guid? supplierBranchId = null,
        string? supplierBranchName = null,
        Guid? intendedReceivingBranchId = null,
        ConnectedPoPaymentTiming paymentTiming = ConnectedPoPaymentTiming.PayBeforeFulfillment)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureLines(lines);
        ValidateExpectedDelivery(orderDate, expectedDeliveryDate);
        if (createdBy is { } actor && actor != Guid.Empty)
        {
            SaleMoney.EnsureActor(actor);
        }
        else
        {
            createdBy = null;
        }

        var poId = id ?? PurchaseOrderId.New();
        var poLines = BuildDraftLines(poId, organizationId, lines);

        return new PurchaseOrder(
            poId,
            organizationId,
            poNumber: null,
            supplierId,
            PurchaseOrderStatus.Draft,
            orderDate,
            expectedDeliveryDate,
            NormalizeSupplierReference(supplierReference),
            NormalizeNotes(notes),
            orderedAtUtc: null,
            orderedBy: createdBy,
            utcNow,
            utcNow,
            poLines,
            paymentTerm,
            paymentTiming,
            supplierBranchId,
            supplierBranchName,
            intendedReceivingBranchId);
    }

    public void UpdateDraft(
        SupplierId supplierId,
        DateOnly orderDate,
        IReadOnlyList<PurchaseOrderLineDraft> lines,
        DateTimeOffset utcNow,
        DateOnly? expectedDeliveryDate = null,
        string? supplierReference = null,
        string? notes = null,
        ConnectedPoPaymentTerm? paymentTerm = null,
        ConnectedPoPaymentTiming? paymentTiming = null,
        Guid? supplierBranchId = null,
        string? supplierBranchName = null,
        bool updateSupplierSourceBranch = false)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureDraft();
        EnsureLines(lines);
        ValidateExpectedDelivery(orderDate, expectedDeliveryDate);

        SupplierId = supplierId;
        OrderDate = orderDate;
        ExpectedDeliveryDate = expectedDeliveryDate;
        SupplierReference = NormalizeSupplierReference(supplierReference);
        Notes = NormalizeNotes(notes);
        if (paymentTerm is { } term)
        {
            PaymentTerm = term;
        }

        if (paymentTiming is { } timing)
        {
            PaymentTiming = timing;
        }

        if (updateSupplierSourceBranch)
        {
            SupplierBranchId = NormalizeBranchId(supplierBranchId);
            SupplierBranchNameSnapshot = NormalizeBranchName(supplierBranchName);
        }

        ReplaceDraftLines(lines);
        UpdatedAtUtc = utcNow;
    }

    public void Submit(
        string poNumber,
        IReadOnlyList<PurchaseOrderLineSnapshotInput> snapshots,
        Guid orderedBy,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(orderedBy);
        EnsureDraft();
        EnsureLines(snapshots);

        if (snapshots.Count != _lines.Count)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                "Line snapshot count must match draft lines.");
        }

        static string LineKey(CatalogProductId? productId, CatalogProductId? supplierProductId) =>
            productId is not null
                ? $"b:{productId.Value:D}"
                : $"s:{supplierProductId!.Value:D}";

        var snapshotByKey = snapshots.ToDictionary(s => LineKey(s.ProductId, s.SupplierProductId));
        foreach (var line in _lines.OrderBy(l => l.LineNumber))
        {
            if (!snapshotByKey.TryGetValue(LineKey(line.ProductId, line.SupplierProductId), out var snapshot))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Each draft line must have a matching snapshot on submit.");
            }

            line.FreezeSnapshot(snapshot);
        }

        PoNumber = PurchaseOrderNumbers.Normalize(poNumber);
        Status = PurchaseOrderStatus.Ordered;
        OrderedAtUtc = utcNow;
        OrderedBy = orderedBy;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(Guid cancelledBy, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(cancelledBy);
        if (Status is PurchaseOrderStatus.Cancelled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Purchase order is already cancelled.");
        }

        if (Status is PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Purchase orders with receipts cannot be cancelled.");
        }

        if (_lines.Any(l => l.ReceivedQty > 0m))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Purchase orders with received quantity cannot be cancelled.");
        }

        Status = PurchaseOrderStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledByUserId = cancelledBy;
        UpdatedAtUtc = utcNow;
    }

    public void ApplyReceiptLines(IReadOnlyList<PurchaseOrderReceiveLineDraft> receiveLines, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only ordered purchase orders can receive goods.");
        }

        if (receiveLines is null || receiveLines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseReceiveRequiresLines,
                "At least one receive line is required.");
        }

        var lineByProduct = _lines
            .Where(l => l.ProductId is not null)
            .ToDictionary(l => l.ProductId!.Value);
        foreach (var receive in receiveLines)
        {
            if (!lineByProduct.TryGetValue(receive.ProductId.Value, out var line))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Receive line product is not on this purchase order.");
            }

            if (line.NeedsBuyerProductSetup)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Product setup is required before goods can be received.");
            }

            if (line.UomSnapshot is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Cannot receive against an unordered line.");
            }

            PurchaseOrderReceiveDiscrepancy.EnsureValid(
                line.OutstandingQty,
                receive,
                line.UomSnapshot.Value,
                receive.SellingMode);

            if (receive.ReceiveQty > 0m)
            {
                line.ApplyReceipt(receive.ReceiveQty, receive.SellingMode);
            }

            if (receive.ShortClosedQty > 0m)
            {
                line.ApplyShortClose(receive.ShortClosedQty, receive.SellingMode);
            }

            if (receive.ReceiveQty <= 0m
                && receive.ShortClosedQty <= 0m
                && receive.DamagedQty <= 0m
                && receive.RejectedQty <= 0m)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                    "Each receive line must include good, damaged, rejected, or short-closed quantity.");
            }
        }

        Status = _lines.All(l => l.OutstandingQty <= 0m)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Unwinds quantities posted by a goods receipt and recomputes Ordered / PartiallyReceived / Received.
    /// </summary>
    public void UnwindGoodsReceipt(GoodsReceipt receipt, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (receipt.PurchaseOrderId != Id || receipt.OrganizationId != OrganizationId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Goods receipt does not belong to this purchase order.");
        }

        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only ordered purchase orders can unwind a goods receipt.");
        }

        var lineById = _lines.ToDictionary(l => l.Id.Value);
        foreach (var grnLine in receipt.Lines)
        {
            if (!lineById.TryGetValue(grnLine.PurchaseOrderLineId.Value, out var poLine))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Goods receipt line does not match a purchase-order line.");
            }

            poLine.ReverseReceipt(grnLine.QuantityReceived, grnLine.ShortClosedQty);
        }

        Status = _lines.All(l => l.OutstandingQty <= 0m)
            ? PurchaseOrderStatus.Received
            : _lines.Any(l => l.ReceivedQty > 0m)
                ? PurchaseOrderStatus.PartiallyReceived
                : PurchaseOrderStatus.Ordered;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// After explicit create/link, bind buyer product onto unlinked connected lines for this supplier product.
    /// </summary>
    public void BindBuyerProductForSupplierProduct(
        CatalogProductId supplierProductId,
        CatalogProductId buyerProductId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        var matched = false;
        foreach (var line in _lines)
        {
            if (line.SupplierProductId != supplierProductId)
            {
                continue;
            }

            line.BindBuyerProduct(buyerProductId);
            matched = true;
        }

        if (!matched)
        {
            return;
        }

        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Caps remaining outstanding to supplier-confirmed quantities without changing OrderedQty.
    /// Reduced/unavailable remainder is short-closed so goods receipt cannot exceed confirmation.
    /// </summary>
    public void AlignOutstandingToConfirmedQuantities(
        IReadOnlyDictionary<Guid, decimal> confirmedQtyByBuyerProductId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only ordered purchase orders can align to confirmed quantities.");
        }

        foreach (var line in _lines)
        {
            if (line.ProductId is null
                || !confirmedQtyByBuyerProductId.TryGetValue(line.ProductId.Value, out var confirmed))
            {
                continue;
            }

            if (confirmed < 0m || confirmed > line.OrderedQty)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                    "Confirmed quantity must be between zero and the original ordered quantity.");
            }

            var allowedRemaining = Math.Max(0m, confirmed - line.ReceivedQty);
            var excess = line.OutstandingQty - allowedRemaining;
            if (excess > 0m)
            {
                line.ApplyShortClose(excess);
            }
        }

        Status = _lines.All(l => l.OutstandingQty <= 0m)
            ? PurchaseOrderStatus.Received
            : _lines.Any(l => l.ReceivedQty > 0m)
                ? PurchaseOrderStatus.PartiallyReceived
                : PurchaseOrderStatus.Ordered;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>True when any line has buyer-closed shortages (Received With Issues signal).</summary>
    public bool HasReceivingIssues => _lines.Any(l => l.HasReceivingIssues);

    /// <summary>
    /// Explicitly cancels all outstanding quantity (seller Close remaining). Does not change OrderedQty
    /// or ReceivedQty. Settlement snapshots are recorded for refund/charge projection.
    /// </summary>
    public void CloseAllRemaining(
        string reason,
        Guid actorId,
        DateTimeOffset utcNow,
        decimal refundDueAmount,
        decimal amountPaid)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "An actor identifier is required to close remaining quantity.");
        }

        if (RemainingClosedAtUtc is not null)
        {
            return;
        }

        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only ordered purchase orders can close remaining quantity.");
        }

        if (_lines.All(l => l.OutstandingQty <= 0m))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "There is no remaining quantity to close.");
        }

        var normalizedReason = NormalizeRemainingClosedReason(reason);
        foreach (var line in _lines)
        {
            if (line.OutstandingQty > 0m)
            {
                line.ApplyShortClose(line.OutstandingQty);
            }
        }

        var finalAccepted = 0m;
        var cancelledValue = 0m;
        foreach (var line in _lines)
        {
            finalAccepted += SaleMoney.RoundMoney(line.ReceivedQty * line.UnitPurchaseCost);
            cancelledValue += SaleMoney.RoundMoney(line.ClosedShortQty * line.UnitPurchaseCost);
        }

        RemainingClosedAtUtc = utcNow;
        RemainingClosedByUserId = actorId;
        RemainingClosedReason = normalizedReason;
        FinalAcceptedValue = SaleMoney.RoundMoney(finalAccepted);
        CancelledRemainingValue = SaleMoney.RoundMoney(cancelledValue);
        RefundDueAmount = refundDueAmount < 0m ? 0m : SaleMoney.RoundMoney(refundDueAmount);
        AmountPaidSnapshot = SaleMoney.RoundMoney(amountPaid);
        Status = PurchaseOrderStatus.Received;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Records authoritative completion settlement when outstanding reaches zero via receipt
    /// (good + short-close on lines), without requiring an explicit Close remaining action.
    /// Does not rewrite payment history; only raises RefundDue when paid exceeds accepted good value.
    /// </summary>
    public void ApplyCompletionSettlement(
        decimal finalAcceptedValue,
        decimal cancelledRemainingValue,
        decimal refundDueAmount,
        decimal amountPaid,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status != PurchaseOrderStatus.Received)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Completion settlement applies only when the purchase order is fully received.");
        }

        FinalAcceptedValue = SaleMoney.RoundMoney(Math.Max(0m, finalAcceptedValue));
        CancelledRemainingValue = SaleMoney.RoundMoney(Math.Max(0m, cancelledRemainingValue));
        var roundedPaid = SaleMoney.RoundMoney(Math.Max(0m, amountPaid));
        var currentPaid = AmountPaidSnapshot ?? 0m;
        AmountPaidSnapshot = SaleMoney.RoundMoney(Math.Max(currentPaid, roundedPaid));
        var nextRefundDue = refundDueAmount < 0m ? 0m : SaleMoney.RoundMoney(refundDueAmount);
        // Never decrease an already recorded refund due (idempotent retries / prior short-close).
        RefundDueAmount = SaleMoney.RoundMoney(Math.Max(RefundDueAmount, nextRefundDue));
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Holds a fully received pay-on-delivery/receipt purchase order in commercial settlement
    /// until the seller confirms payment. Goods remain <see cref="PurchaseOrderStatus.Received"/>.
    /// Idempotent; never downgrades an already settled order.
    /// </summary>
    /// <param name="effectivePaymentTiming">
    /// Connected-relationship effective timing when it supersedes <see cref="PaymentTiming"/>
    /// (seller-proposed timing accepted by the buyer).
    /// </param>
    public void MarkAwaitingPayment(
        DateTimeOffset utcNow,
        ConnectedPoPaymentTiming? effectivePaymentTiming = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status != PurchaseOrderStatus.Received)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Awaiting payment applies only when the purchase order is fully received.");
        }

        if ((effectivePaymentTiming ?? PaymentTiming) != ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Awaiting payment applies only to pay-on-delivery or pay-on-receipt timing.");
        }

        if (FinancialSettlementStatus is ConnectedPoFinancialSettlementStatus.Settled
            or ConnectedPoFinancialSettlementStatus.AwaitingPayment)
        {
            return;
        }

        FinancialSettlementStatus = ConnectedPoFinancialSettlementStatus.AwaitingPayment;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Records seller-confirmed settlement. Idempotent: a second confirmation preserves the first
    /// settlement timestamp, actor, and remarks.
    /// </summary>
    public void MarkFinanciallySettled(Guid actorId, DateTimeOffset utcNow, string? sellerRemarks = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        if (Status != PurchaseOrderStatus.Received)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Settlement can be confirmed only when the purchase order is fully received.");
        }

        if (FinancialSettlementStatus == ConnectedPoFinancialSettlementStatus.Settled)
        {
            return;
        }

        FinancialSettlementStatus = ConnectedPoFinancialSettlementStatus.Settled;
        SellerSettlementRemarks = NormalizeSellerSettlementRemarks(sellerRemarks) ?? SellerSettlementRemarks;
        FinanciallySettledAtUtc = utcNow;
        FinanciallySettledBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Auto-completes settlement when nothing is due (zero accepted value, or already fully paid).
    /// No actor is recorded because no payment was collected at this point.
    /// </summary>
    public void MarkNoPaymentDue(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status != PurchaseOrderStatus.Received)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "No-payment-due completion applies only when the purchase order is fully received.");
        }

        if (FinancialSettlementStatus == ConnectedPoFinancialSettlementStatus.Settled)
        {
            return;
        }

        FinancialSettlementStatus = ConnectedPoFinancialSettlementStatus.Settled;
        FinanciallySettledAtUtc ??= utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Records a settlement payment collected after goods receipt (pay-on-delivery/receipt).
    /// Cumulative and never decreasing; does not by itself confirm settlement.
    /// </summary>
    public void RecordPostReceiptSettlementPayment(
        decimal settledAmount,
        DateTimeOffset utcNow,
        string? sellerRemarks = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        var rounded = SaleMoney.RoundMoney(settledAmount);
        if (rounded < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Settlement payment cannot be negative.");
        }

        AmountPaidSnapshot = SaleMoney.RoundMoney((AmountPaidSnapshot ?? 0m) + rounded);
        SellerSettlementRemarks = NormalizeSellerSettlementRemarks(sellerRemarks) ?? SellerSettlementRemarks;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeRemainingClosedReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderNotes,
                "A reason is required to close remaining quantity.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > RemainingClosedReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderNotes,
                $"Close remaining reason must be at most {RemainingClosedReasonMaxLength} characters.");
        }

        return trimmed;
    }

    public static PurchaseOrder Rehydrate(
        PurchaseOrderId id,
        PosOrganizationId organizationId,
        string? poNumber,
        SupplierId supplierId,
        PurchaseOrderStatus status,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        string? supplierReference,
        string? notes,
        DateTimeOffset? orderedAtUtc,
        Guid? orderedBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IReadOnlyList<PurchaseOrderLine> lines,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash,
        ConnectedPoPaymentTiming paymentTiming = ConnectedPoPaymentTiming.PayBeforeFulfillment,
        Guid? supplierBranchId = null,
        string? supplierBranchNameSnapshot = null,
        Guid? intendedReceivingBranchId = null,
        DateTimeOffset? cancelledAtUtc = null,
        Guid? cancelledByUserId = null,
        DateTimeOffset? remainingClosedAtUtc = null,
        Guid? remainingClosedByUserId = null,
        string? remainingClosedReason = null,
        decimal? finalAcceptedValue = null,
        decimal? cancelledRemainingValue = null,
        decimal refundDueAmount = 0m,
        decimal? amountPaidSnapshot = null,
        ConnectedPoFinancialSettlementStatus financialSettlementStatus =
            ConnectedPoFinancialSettlementStatus.NotRequired,
        string? sellerSettlementRemarks = null,
        DateTimeOffset? financiallySettledAtUtc = null,
        Guid? financiallySettledBy = null) =>
        new(
            id,
            organizationId,
            poNumber,
            supplierId,
            status,
            orderDate,
            expectedDeliveryDate,
            supplierReference,
            notes,
            orderedAtUtc,
            orderedBy,
            createdAtUtc,
            updatedAtUtc,
            lines.ToList(),
            paymentTerm,
            paymentTiming,
            supplierBranchId,
            supplierBranchNameSnapshot,
            intendedReceivingBranchId,
            cancelledAtUtc,
            cancelledByUserId,
            remainingClosedAtUtc,
            remainingClosedByUserId,
            remainingClosedReason,
            finalAcceptedValue,
            cancelledRemainingValue,
            refundDueAmount,
            amountPaidSnapshot,
            financialSettlementStatus,
            sellerSettlementRemarks,
            financiallySettledAtUtc,
            financiallySettledBy);

    public static string? NormalizeSellerSettlementRemarks(string? remarks) =>
        NormalizeOptionalText(
            remarks,
            SellerSettlementRemarksMaxLength,
            DomainErrorCodes.InvalidPurchaseOrderNotes,
            "Seller settlement remarks");

    private static Guid? NormalizeBranchId(Guid? branchId) =>
        branchId is null || branchId == Guid.Empty ? null : branchId;

    private static string? NormalizeBranchName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        return trimmed.Length <= 128 ? trimmed : trimmed[..128];
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only draft purchase orders can be edited.");
        }
    }

    private void ReplaceDraftLines(IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        _lines.Clear();
        _lines.AddRange(BuildDraftLines(Id, OrganizationId, lines));
    }

    private static List<PurchaseOrderLine> BuildDraftLines(
        PurchaseOrderId poId,
        PosOrganizationId organizationId,
        IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        EnsureNoDuplicateProducts(lines);
        var result = new List<PurchaseOrderLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            result.Add(PurchaseOrderLine.CreateDraft(poId, organizationId, i + 1, lines[i]));
        }

        return result;
    }

    private static void EnsureLines(IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                "A purchase order must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                $"A purchase order may contain at most {MaxLineCount} lines.");
        }

        EnsureNoDuplicateProducts(lines);
    }

    private static void EnsureLines(IReadOnlyList<PurchaseOrderLineSnapshotInput> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                "A purchase order must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                $"A purchase order may contain at most {MaxLineCount} lines.");
        }

        var productIds = lines
            .Where(l => l.ProductId is not null)
            .Select(l => l.ProductId!.Value)
            .ToList();
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate products are not allowed on a purchase order.");
        }

        var supplierProductIds = lines
            .Where(l => l.SupplierProductId is not null)
            .Select(l => l.SupplierProductId!.Value)
            .ToList();
        if (supplierProductIds.Count != supplierProductIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate supplier products are not allowed on a purchase order.");
        }
    }

    private static void EnsureNoDuplicateProducts(IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        var productIds = lines
            .Where(l => l.ProductId is not null)
            .Select(l => l.ProductId!.Value)
            .ToList();
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate products are not allowed on a purchase order.");
        }

        var supplierProductIds = lines
            .Where(l => l.SupplierProductId is not null)
            .Select(l => l.SupplierProductId!.Value)
            .ToList();
        if (supplierProductIds.Count != supplierProductIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate supplier products are not allowed on a purchase order.");
        }

        foreach (var line in lines)
        {
            if (line.ProductId is null && line.SupplierProductId is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "A purchase-order line requires a buyer product or a supplier product identity.");
            }
        }
    }

    private static void ValidateExpectedDelivery(DateOnly orderDate, DateOnly? expectedDeliveryDate)
    {
        if (expectedDeliveryDate is not null && expectedDeliveryDate.Value < orderDate)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseExpectedDeliveryDate,
                "Expected delivery date cannot be before order date.");
        }
    }

    private static string? NormalizeSupplierReference(string? value) =>
        NormalizeOptionalText(value, SupplierReferenceMaxLength, DomainErrorCodes.InvalidPurchaseSupplierReference, "Supplier reference");

    private static string? NormalizeNotes(string? value) =>
        NormalizeOptionalText(value, NotesMaxLength, DomainErrorCodes.InvalidPurchaseOrderNotes, "Notes");

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{label} must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
