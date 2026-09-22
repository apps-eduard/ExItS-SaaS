using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using System.Text.Json;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public sealed record ReturnBatchAcceptedLineDraft(
    SaleLineId SaleLineId,
    decimal AcceptedQuantity);

/// <summary>Buyer-requested return quantity against one line of a completed connected purchase order.</summary>
public sealed record ReturnBatchConnectedPoLineDraft(
    PurchaseOrderLineId PurchaseOrderLineId,
    decimal AcceptedQuantity);

public sealed class ReturnBatch
{
    public const int ReasonMaxLength = 512;
    public const int NotesMaxLength = 512;
    public const int InspectionNoteMaxLength = 256;
    public const int PoNumberSnapshotMaxLength = 32;
    public const int MaxLineCount = Sale.MaxLineCount;

    private readonly List<ReturnBatchLine> _lines;
    private readonly List<ReturnBatchRefund> _refunds;
    private readonly List<ReturnBatchAuditEvent> _timeline;

    public ReturnBatchId Id { get; }
    /// <summary>Owning organization. Sale returns = selling org. Connected-PO returns = seller (supplier) org.</summary>
    public PosOrganizationId OrganizationId { get; }
    public ReturnBatchSourceType SourceType { get; }
    /// <summary>Required for <see cref="ReturnBatchSourceType.Sale"/>; always null for connected-PO returns.</summary>
    public SaleId? SaleId { get; }
    public PosBranchId? BranchId { get; }
    public ConnectedPurchaseOrderId? ConnectedPurchaseOrderId { get; }
    public PurchaseOrderId? PurchaseOrderId { get; }
    public PosOrganizationId? BuyerOrganizationId { get; }
    public PosOrganizationId? SellerOrganizationId { get; }
    public PosBranchId? BuyerBranchId { get; }
    public PosBranchId? SellerBranchId { get; }
    /// <summary>Payment timing snapshot at return-request time; drives settlement without re-reading the PO.</summary>
    public ConnectedPoPaymentTiming? PaymentTimingSnapshot { get; }
    public string? PoNumberSnapshot { get; }
    public string BatchNumber { get; }
    public ReturnBatchStatus Status { get; private set; }
    public ReturnBatchRefundStatus RefundStatus { get; private set; }
    public decimal AcceptedReturnValue { get; }
    public decimal RefundDueAmount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public SaleReturnId? SaleReturnId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SellerReceivedAtUtc { get; private set; }
    public Guid? SellerReceivedBy { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }
    public Guid? FinalizedBy { get; private set; }
    public string Reason { get; }
    public string? Notes { get; }

    public IReadOnlyList<ReturnBatchLine> Lines => _lines;
    public IReadOnlyList<ReturnBatchRefund> Refunds => _refunds;
    public IReadOnlyList<ReturnBatchAuditEvent> Timeline => _timeline;

    public bool IsConnectedPurchaseOrderReturn => SourceType == ReturnBatchSourceType.ConnectedPurchaseOrder;

    private ReturnBatch(
        ReturnBatchId id,
        PosOrganizationId organizationId,
        ReturnBatchSourceType sourceType,
        SaleId? saleId,
        PosBranchId? branchId,
        string batchNumber,
        ReturnBatchStatus status,
        ReturnBatchRefundStatus refundStatus,
        decimal acceptedReturnValue,
        decimal refundDueAmount,
        decimal refundedAmount,
        SaleReturnId? saleReturnId,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? finalizedAtUtc,
        Guid? finalizedBy,
        string reason,
        string? notes,
        List<ReturnBatchLine> lines,
        List<ReturnBatchRefund>? refunds = null,
        List<ReturnBatchAuditEvent>? timeline = null,
        ConnectedPurchaseOrderId? connectedPurchaseOrderId = null,
        PurchaseOrderId? purchaseOrderId = null,
        PosOrganizationId? buyerOrganizationId = null,
        PosOrganizationId? sellerOrganizationId = null,
        PosBranchId? buyerBranchId = null,
        PosBranchId? sellerBranchId = null,
        ConnectedPoPaymentTiming? paymentTimingSnapshot = null,
        string? poNumberSnapshot = null,
        DateTimeOffset? sellerReceivedAtUtc = null,
        Guid? sellerReceivedBy = null)
    {
        if (sourceType == ReturnBatchSourceType.Sale && saleId is null)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchSourceMismatch,
                "Sale return batches require a sale identifier.");
        }

        if (sourceType == ReturnBatchSourceType.ConnectedPurchaseOrder
            && (purchaseOrderId is null || buyerOrganizationId is null || sellerOrganizationId is null))
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchSourceMismatch,
                "Connected purchase-order return batches require the purchase order and both organizations.");
        }

        Id = id;
        OrganizationId = organizationId;
        SourceType = sourceType;
        SaleId = saleId;
        BranchId = branchId;
        BatchNumber = batchNumber;
        Status = status;
        RefundStatus = refundStatus;
        AcceptedReturnValue = acceptedReturnValue;
        RefundDueAmount = refundDueAmount;
        RefundedAmount = refundedAmount;
        SaleReturnId = saleReturnId;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
        UpdatedAtUtc = updatedAtUtc;
        FinalizedAtUtc = finalizedAtUtc;
        FinalizedBy = finalizedBy;
        Reason = reason;
        Notes = notes;
        ConnectedPurchaseOrderId = connectedPurchaseOrderId;
        PurchaseOrderId = purchaseOrderId;
        BuyerOrganizationId = buyerOrganizationId;
        SellerOrganizationId = sellerOrganizationId;
        BuyerBranchId = buyerBranchId;
        SellerBranchId = sellerBranchId;
        PaymentTimingSnapshot = paymentTimingSnapshot;
        PoNumberSnapshot = NormalizePoNumberSnapshot(poNumberSnapshot);
        SellerReceivedAtUtc = sellerReceivedAtUtc;
        SellerReceivedBy = sellerReceivedBy;
        _lines = lines;
        _refunds = refunds ?? [];
        _timeline = timeline ?? [];
    }

    public static ReturnBatch CreateAccepted(
        PosOrganizationId organizationId,
        string batchNumber,
        Sale sale,
        IReadOnlyList<ReturnBatchAcceptedLineDraft> lineDrafts,
        IReadOnlyDictionary<Guid, (decimal ReturnedQuantity, decimal RefundedAmount)> priorBySaleLineId,
        string reason,
        Guid createdBy,
        DateTimeOffset utcNow,
        string? notes = null,
        ReturnBatchId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdBy);

        if (sale.OrganizationId != organizationId)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnOrganizationMismatch,
                "Return batch must belong to the same organization as the sale.");
        }

        if (sale.Status != SaleStatus.Completed)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchSaleNotReturnable,
                "Only completed sales can be accepted for return inspection.");
        }

        EnsureDraftCount(lineDrafts?.Count ?? 0);

        var saleLinesById = sale.Lines.ToDictionary(l => l.Id.Value);
        var consolidated = ConsolidateDrafts(lineDrafts!);
        var returnBatchId = id ?? ReturnBatchId.New();
        var lines = new List<ReturnBatchLine>(consolidated.Count);

        foreach (var draft in consolidated)
        {
            if (!saleLinesById.TryGetValue(draft.SaleLineId.Value, out var saleLine))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidReturnBatchLine,
                    "Return batch line must reference a sale line from the originating sale.");
            }

            priorBySaleLineId.TryGetValue(saleLine.Id.Value, out var prior);
            lines.Add(ReturnBatchLine.CreateAccepted(
                returnBatchId,
                organizationId,
                saleLine,
                draft,
                prior.ReturnedQuantity,
                prior.RefundedAmount));
        }

        var acceptedReturnValue = EnsurePositiveAcceptedValue(lines);
        var created = new ReturnBatch(
            returnBatchId,
            organizationId,
            ReturnBatchSourceType.Sale,
            sale.Id,
            sale.BranchId,
            ReturnBatchNumbers.Normalize(batchNumber),
            ReturnBatchStatus.PendingInspection,
            ReturnBatchRefundStatus.None,
            acceptedReturnValue,
            refundDueAmount: 0m,
            refundedAmount: 0m,
            saleReturnId: null,
            createdAtUtc: utcNow,
            createdBy,
            updatedAtUtc: utcNow,
            finalizedAtUtc: null,
            finalizedBy: null,
            NormalizeReason(reason),
            NormalizeNotes(notes),
            lines);
        created.AddTimelineEvent(
            ReturnBatchAuditEventType.Accepted,
            JsonSerializer.Serialize(new
            {
                acceptedReturnValue = created.AcceptedReturnValue,
                acceptedLineCount = created.Lines.Count,
                created.Reason,
                created.Notes
            }),
            createdBy,
            utcNow);
        return created;
    }

    /// <summary>
    /// Buyer-requested return against a completed (fully received) connected purchase order.
    /// The batch is owned by the seller organization and starts in
    /// <see cref="ReturnBatchStatus.AwaitingSellerReceipt"/> until the seller confirms physical receipt.
    /// Return value uses the locked unit purchase cost, never the classified sellable split.
    /// </summary>
    public static ReturnBatch CreateAcceptedForConnectedPurchaseOrder(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        string batchNumber,
        PurchaseOrder buyerPurchaseOrder,
        IReadOnlyList<ReturnBatchConnectedPoLineDraft> lineDrafts,
        IReadOnlyDictionary<Guid, decimal> priorReturnedByPurchaseOrderLineId,
        string reason,
        Guid createdBy,
        DateTimeOffset utcNow,
        ConnectedPurchaseOrderId? connectedPurchaseOrderId = null,
        ConnectedPoPaymentTiming? paymentTiming = null,
        PosBranchId? buyerBranchId = null,
        PosBranchId? sellerBranchId = null,
        string? notes = null,
        ReturnBatchId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdBy);
        ArgumentNullException.ThrowIfNull(buyerPurchaseOrder);

        if (buyerPurchaseOrder.OrganizationId != buyerOrganizationId)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchSourceMismatch,
                "Purchase order must belong to the buyer organization.");
        }

        if (buyerOrganizationId == sellerOrganizationId)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchSourceMismatch,
                "Connected purchase-order returns require distinct buyer and seller organizations.");
        }

        if (buyerPurchaseOrder.Status != PurchaseOrderStatus.Received)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchPurchaseOrderNotReturnable,
                "Only completed (fully received) purchase orders can be returned.");
        }

        EnsureDraftCount(lineDrafts?.Count ?? 0);

        var poLinesById = buyerPurchaseOrder.Lines.ToDictionary(l => l.Id.Value);
        var consolidated = ConsolidateConnectedPoDrafts(lineDrafts!);
        var returnBatchId = id ?? ReturnBatchId.New();
        var lines = new List<ReturnBatchLine>(consolidated.Count);

        foreach (var draft in consolidated)
        {
            if (!poLinesById.TryGetValue(draft.PurchaseOrderLineId.Value, out var poLine))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidReturnBatchLine,
                    "Return batch line must reference a line from the originating purchase order.");
            }

            priorReturnedByPurchaseOrderLineId.TryGetValue(poLine.Id.Value, out var priorReturned);
            lines.Add(ReturnBatchLine.CreateAcceptedFromPurchaseOrderLine(
                returnBatchId,
                sellerOrganizationId,
                poLine,
                draft,
                priorReturned));
        }

        var acceptedReturnValue = EnsurePositiveAcceptedValue(lines);
        var created = new ReturnBatch(
            returnBatchId,
            sellerOrganizationId,
            ReturnBatchSourceType.ConnectedPurchaseOrder,
            saleId: null,
            branchId: sellerBranchId,
            ReturnBatchNumbers.Normalize(batchNumber),
            ReturnBatchStatus.AwaitingSellerReceipt,
            ReturnBatchRefundStatus.None,
            acceptedReturnValue,
            refundDueAmount: 0m,
            refundedAmount: 0m,
            saleReturnId: null,
            createdAtUtc: utcNow,
            createdBy,
            updatedAtUtc: utcNow,
            finalizedAtUtc: null,
            finalizedBy: null,
            NormalizeReason(reason),
            NormalizeNotes(notes),
            lines,
            refunds: null,
            timeline: null,
            connectedPurchaseOrderId,
            buyerPurchaseOrder.Id,
            buyerOrganizationId,
            sellerOrganizationId,
            buyerBranchId,
            sellerBranchId,
            paymentTiming ?? buyerPurchaseOrder.PaymentTiming,
            buyerPurchaseOrder.PoNumber);
        created.AddTimelineEvent(
            ReturnBatchAuditEventType.Accepted,
            JsonSerializer.Serialize(new
            {
                sourceType = ReturnBatchSourceTypes.ToCode(ReturnBatchSourceType.ConnectedPurchaseOrder),
                purchaseOrderId = buyerPurchaseOrder.Id.Value,
                poNumber = created.PoNumberSnapshot,
                acceptedReturnValue = created.AcceptedReturnValue,
                acceptedLineCount = created.Lines.Count,
                created.Reason,
                created.Notes
            }),
            createdBy,
            utcNow);
        return created;
    }

    /// <summary>
    /// Seller confirms physical receipt of the returned goods. Idempotent once past
    /// <see cref="ReturnBatchStatus.AwaitingSellerReceipt"/>.
    /// </summary>
    public bool MarkReceivedBySeller(Guid actorId, DateTimeOffset utcNow)
    {
        EnsureNotFinalized();
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (!IsConnectedPurchaseOrderReturn)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchSourceMismatch,
                "Only connected purchase-order returns are received by a seller.");
        }

        if (Status != ReturnBatchStatus.AwaitingSellerReceipt)
        {
            return false;
        }

        Status = ReturnBatchStatus.PendingInspection;
        SellerReceivedAtUtc = utcNow;
        SellerReceivedBy = actorId;
        UpdatedAtUtc = utcNow;
        AddTimelineEvent(
            ReturnBatchAuditEventType.ReceivedBySeller,
            JsonSerializer.Serialize(new
            {
                acceptedReturnValue = AcceptedReturnValue,
                acceptedLineCount = _lines.Count,
                returnedQuantity = _lines.Sum(l => l.AcceptedQuantity)
            }),
            actorId,
            utcNow);
        return true;
    }

    public void ClassifyLine(
        ReturnBatchLineId lineId,
        decimal sellableQuantity,
        decimal damagedQuantity,
        string? inspectionNote,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        EnsureNotFinalized();
        EnsureInspectable();
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchLineNotFound,
                "Return batch line was not found.");
        }

        line.Classify(sellableQuantity, damagedQuantity, inspectionNote, actorId, utcNow);
        AddTimelineEvent(
            ReturnBatchAuditEventType.ClassificationSaved,
            JsonSerializer.Serialize(new
            {
                returnBatchLineId = line.Id.Value,
                line.SellableQuantity,
                line.DamagedQuantity,
                line.InspectionNote
            }),
            actorId,
            utcNow);
        UpdatedAtUtc = utcNow;
        Status = AllLinesClassified
            ? ReturnBatchStatus.ReadyForFinalize
            : ReturnBatchStatus.PendingInspection;
    }

    public bool AllLinesClassified => _lines.All(l => l.IsClassified);

    public void MarkFinalized(
        SaleReturnId? saleReturnId,
        ReturnBatchRefundStatus refundStatus,
        decimal refundDueAmount,
        decimal refundedAmount,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        EnsureNotFinalized();
        EnsureInspectable();
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (!AllLinesClassified)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchNotReadyForFinalize,
                "All return batch lines must be classified before finalizing.");
        }

        if (refundDueAmount < 0m || refundedAmount < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchRefundAmount,
                "Refund due and refunded amounts must be zero or greater.");
        }

        var roundedDue = SaleMoney.RoundMoney(refundDueAmount);
        var roundedRefunded = SaleMoney.RoundMoney(refundedAmount);
        if (SaleMoney.RoundMoney(roundedDue + roundedRefunded) > AcceptedReturnValue)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchRefundAmount,
                "Refund totals cannot exceed accepted return value.");
        }

        SaleReturnId = saleReturnId;
        RefundStatus = refundStatus;
        RefundDueAmount = roundedDue;
        RefundedAmount = roundedRefunded;
        Status = ReturnBatchStatus.Finalized;
        FinalizedAtUtc = utcNow;
        FinalizedBy = actorId;
        UpdatedAtUtc = utcNow;
        AddTimelineEvent(
            ReturnBatchAuditEventType.Finalized,
            JsonSerializer.Serialize(new
            {
                refundStatus = ReturnBatchRefundStatuses.ToCode(refundStatus),
                refundDueAmount = roundedDue,
                refundedAmount = roundedRefunded,
                acceptedReturnValue = AcceptedReturnValue
            }),
            actorId,
            utcNow);
    }

    public ReturnBatchRefund RecordRefund(
        decimal amount,
        SalePaymentMethod method,
        string? reference,
        string? note,
        Guid actorId,
        DateTimeOffset utcNow,
        string? clientRefundId = null,
        ReturnBatchRefundId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (Status != ReturnBatchStatus.Finalized || RefundDueAmount <= RefundedAmount)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchRefundNotAllowed,
                "Refund can only be recorded on finalized batches with refund remaining.");
        }

        if (amount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchRefundAmount,
                "Refund amount must be greater than zero.");
        }

        if (method == SalePaymentMethod.Utang)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchRefundMethod,
                "Utang is not a valid manual refund method.");
        }

        var remaining = SaleMoney.RoundMoney(RefundDueAmount - RefundedAmount);
        var rounded = SaleMoney.RoundMoney(amount);
        if (rounded > remaining)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchRefundExceedsRemaining,
                "Refund amount exceeds remaining refund due.");
        }

        var existing = !string.IsNullOrWhiteSpace(clientRefundId)
            ? _refunds.FirstOrDefault(r => string.Equals(r.ClientRefundId, clientRefundId.Trim(), StringComparison.Ordinal))
            : null;
        if (existing is not null)
        {
            return existing;
        }

        var refund = ReturnBatchRefund.Create(
            Id,
            rounded,
            method,
            reference,
            note,
            actorId,
            utcNow,
            clientRefundId,
            id);
        _refunds.Add(refund);

        RefundedAmount = SaleMoney.RoundMoney(RefundedAmount + refund.Amount);
        if (SaleMoney.RoundMoney(RefundDueAmount - RefundedAmount) <= 0m)
        {
            RefundedAmount = RefundDueAmount;
            RefundStatus = ReturnBatchRefundStatus.Refunded;
        }

        UpdatedAtUtc = utcNow;
        AddTimelineEvent(
            ReturnBatchAuditEventType.RefundRecorded,
            JsonSerializer.Serialize(new
            {
                refundAmount = refund.Amount,
                refundMethod = SalePaymentMethods.ToCode(refund.Method),
                refundReference = refund.Reference
            }),
            actorId,
            utcNow);
        return refund;
    }

    public void EnsureNotFinalized()
    {
        if (Status == ReturnBatchStatus.Finalized)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchAlreadyFinalized,
                "Return batch has already been finalized.");
        }
    }

    /// <summary>Organizations allowed to read this batch (buyer and seller for connected-PO returns).</summary>
    public bool IsVisibleTo(PosOrganizationId organizationId) =>
        OrganizationId == organizationId
        || BuyerOrganizationId == organizationId
        || SellerOrganizationId == organizationId;

    public static ReturnBatch Rehydrate(
        ReturnBatchId id,
        PosOrganizationId organizationId,
        SaleId? saleId,
        PosBranchId? branchId,
        string batchNumber,
        ReturnBatchStatus status,
        ReturnBatchRefundStatus refundStatus,
        decimal acceptedReturnValue,
        decimal refundDueAmount,
        decimal refundedAmount,
        SaleReturnId? saleReturnId,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? finalizedAtUtc,
        Guid? finalizedBy,
        string reason,
        string? notes,
        IEnumerable<ReturnBatchLine> lines,
        IEnumerable<ReturnBatchRefund>? refunds = null,
        IEnumerable<ReturnBatchAuditEvent>? timeline = null,
        ReturnBatchSourceType sourceType = ReturnBatchSourceType.Sale,
        ConnectedPurchaseOrderId? connectedPurchaseOrderId = null,
        PurchaseOrderId? purchaseOrderId = null,
        PosOrganizationId? buyerOrganizationId = null,
        PosOrganizationId? sellerOrganizationId = null,
        PosBranchId? buyerBranchId = null,
        PosBranchId? sellerBranchId = null,
        ConnectedPoPaymentTiming? paymentTimingSnapshot = null,
        string? poNumberSnapshot = null,
        DateTimeOffset? sellerReceivedAtUtc = null,
        Guid? sellerReceivedBy = null) =>
        new(
            id,
            organizationId,
            sourceType,
            saleId,
            branchId,
            batchNumber,
            status,
            refundStatus,
            acceptedReturnValue,
            refundDueAmount,
            refundedAmount,
            saleReturnId,
            createdAtUtc,
            createdBy,
            updatedAtUtc,
            finalizedAtUtc,
            finalizedBy,
            reason,
            notes,
            lines.ToList(),
            refunds?.ToList() ?? [],
            timeline?.ToList() ?? [],
            connectedPurchaseOrderId,
            purchaseOrderId,
            buyerOrganizationId,
            sellerOrganizationId,
            buyerBranchId,
            sellerBranchId,
            paymentTimingSnapshot,
            poNumberSnapshot,
            sellerReceivedAtUtc,
            sellerReceivedBy);

    private void EnsureInspectable()
    {
        if (Status == ReturnBatchStatus.AwaitingSellerReceipt)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchAwaitingSellerReceipt,
                "The seller must confirm physical receipt before the return can be inspected.");
        }
    }

    private void AddTimelineEvent(
        ReturnBatchAuditEventType eventType,
        string payloadJson,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        _timeline.Add(ReturnBatchAuditEvent.Create(
            Id,
            eventType,
            payloadJson,
            actorId,
            utcNow));
    }

    private static void EnsureDraftCount(int count)
    {
        if (count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchRequiresAtLeastOneLine,
                "A return batch must contain at least one line.");
        }

        if (count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchRequiresAtLeastOneLine,
                $"A return batch may contain at most {MaxLineCount} lines.");
        }
    }

    private static decimal EnsurePositiveAcceptedValue(List<ReturnBatchLine> lines)
    {
        var acceptedReturnValue = SaleMoney.RoundMoney(lines.Sum(l => l.RefundAmountSnapshot));
        if (acceptedReturnValue <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchAcceptedValue,
                "Accepted return value must be greater than zero.");
        }

        return acceptedReturnValue;
    }

    private static List<ReturnBatchAcceptedLineDraft> ConsolidateDrafts(
        IReadOnlyList<ReturnBatchAcceptedLineDraft> drafts)
    {
        var order = new List<Guid>();
        var quantities = new Dictionary<Guid, decimal>();
        foreach (var draft in drafts.OrderBy(d => d.SaleLineId.Value))
        {
            var key = draft.SaleLineId.Value;
            if (!quantities.ContainsKey(key))
            {
                order.Add(key);
                quantities[key] = 0m;
            }

            quantities[key] += draft.AcceptedQuantity;
        }

        return order
            .Select(id => new ReturnBatchAcceptedLineDraft(SaleLineId.From(id), quantities[id]))
            .ToList();
    }

    private static List<ReturnBatchConnectedPoLineDraft> ConsolidateConnectedPoDrafts(
        IReadOnlyList<ReturnBatchConnectedPoLineDraft> drafts)
    {
        var order = new List<Guid>();
        var quantities = new Dictionary<Guid, decimal>();
        foreach (var draft in drafts.OrderBy(d => d.PurchaseOrderLineId.Value))
        {
            var key = draft.PurchaseOrderLineId.Value;
            if (!quantities.ContainsKey(key))
            {
                order.Add(key);
                quantities[key] = 0m;
            }

            quantities[key] += draft.AcceptedQuantity;
        }

        return order
            .Where(id => quantities[id] > 0m)
            .Select(id => new ReturnBatchConnectedPoLineDraft(PurchaseOrderLineId.From(id), quantities[id]))
            .ToList();
    }

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchReason,
                "A return batch reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchReason,
                $"Return batch reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        return trimmed.Length > NotesMaxLength ? trimmed[..NotesMaxLength] : trimmed;
    }

    private static string? NormalizePoNumberSnapshot(string? poNumber)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
        {
            return null;
        }

        var trimmed = poNumber.Trim();
        return trimmed.Length > PoNumberSnapshotMaxLength ? trimmed[..PoNumberSnapshotMaxLength] : trimmed;
    }
}

public sealed class ReturnBatchLine
{
    public const int NameSnapshotMaxLength = SaleLine.NameSnapshotMaxLength;

    public ReturnBatchLineId Id { get; }
    public ReturnBatchId ReturnBatchId { get; }
    public PosOrganizationId OrganizationId { get; }
    /// <summary>Set for sale-sourced lines only.</summary>
    public SaleLineId? SaleLineId { get; }
    /// <summary>Set for connected purchase-order sourced lines only.</summary>
    public PurchaseOrderLineId? PurchaseOrderLineId { get; }
    /// <summary>Buyer-side catalog product (buyer inventory key).</summary>
    public CatalogProductId ProductId { get; }
    /// <summary>Seller-side catalog product for connected-PO returns (seller inventory key).</summary>
    public CatalogProductId? SupplierProductId { get; }
    public string ProductNameSnapshot { get; }
    public UnitOfMeasure UomSnapshot { get; }
    public decimal UnitPriceSnapshot { get; }
    public decimal LineTotalSnapshot { get; }
    public decimal AcceptedQuantity { get; }
    public decimal RefundAmountSnapshot { get; }
    public decimal? SellableQuantity { get; private set; }
    public decimal? DamagedQuantity { get; private set; }
    public string? InspectionNote { get; private set; }
    public DateTimeOffset? ClassifiedAtUtc { get; private set; }
    public Guid? ClassifiedBy { get; private set; }

    public bool IsClassified => SellableQuantity is not null && DamagedQuantity is not null;

    private ReturnBatchLine(
        ReturnBatchLineId id,
        ReturnBatchId returnBatchId,
        PosOrganizationId organizationId,
        SaleLineId? saleLineId,
        CatalogProductId productId,
        string productNameSnapshot,
        UnitOfMeasure uomSnapshot,
        decimal unitPriceSnapshot,
        decimal lineTotalSnapshot,
        decimal acceptedQuantity,
        decimal refundAmountSnapshot,
        decimal? sellableQuantity,
        decimal? damagedQuantity,
        string? inspectionNote,
        DateTimeOffset? classifiedAtUtc,
        Guid? classifiedBy,
        PurchaseOrderLineId? purchaseOrderLineId = null,
        CatalogProductId? supplierProductId = null)
    {
        if (saleLineId is null && purchaseOrderLineId is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchLine,
                "A return batch line requires a sale line or a purchase-order line reference.");
        }

        Id = id;
        ReturnBatchId = returnBatchId;
        OrganizationId = organizationId;
        SaleLineId = saleLineId;
        PurchaseOrderLineId = purchaseOrderLineId;
        ProductId = productId;
        SupplierProductId = supplierProductId;
        ProductNameSnapshot = productNameSnapshot;
        UomSnapshot = uomSnapshot;
        UnitPriceSnapshot = unitPriceSnapshot;
        LineTotalSnapshot = lineTotalSnapshot;
        AcceptedQuantity = acceptedQuantity;
        RefundAmountSnapshot = refundAmountSnapshot;
        SellableQuantity = sellableQuantity;
        DamagedQuantity = damagedQuantity;
        InspectionNote = inspectionNote;
        ClassifiedAtUtc = classifiedAtUtc;
        ClassifiedBy = classifiedBy;
    }

    internal static ReturnBatchLine CreateAccepted(
        ReturnBatchId returnBatchId,
        PosOrganizationId organizationId,
        SaleLine saleLine,
        ReturnBatchAcceptedLineDraft draft,
        decimal previouslyReturnedQuantity,
        decimal previouslyRefundedAmount)
    {
        if (saleLine.Id != draft.SaleLineId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchLine,
                "Return batch line must reference a line from the originating sale.");
        }

        var acceptedQuantity = SaleLine.NormalizeQuantity(
            draft.AcceptedQuantity,
            saleLine.UnitOfMeasureSnapshot,
            saleLine.SellingModeSnapshot);
        var refundableQuantity = SaleReturnRefundable.RefundableQuantity(saleLine, previouslyReturnedQuantity);
        if (acceptedQuantity > refundableQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchQuantityExceedsRefundable,
                "Accepted quantity exceeds refundable quantity for the sale line.");
        }

        var refundAmount = SaleReturnRefundable.ComputeRefundAmount(
            saleLine,
            acceptedQuantity,
            previouslyReturnedQuantity,
            previouslyRefundedAmount);
        if (refundAmount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchAcceptedValue,
                "Accepted refund value must be greater than zero.");
        }

        return new ReturnBatchLine(
            ReturnBatchLineId.New(),
            returnBatchId,
            organizationId,
            saleLine.Id,
            saleLine.ProductId,
            saleLine.NameSnapshot,
            saleLine.UnitOfMeasureSnapshot,
            saleLine.UnitPrice,
            saleLine.LineTotal,
            acceptedQuantity,
            refundAmount,
            sellableQuantity: null,
            damagedQuantity: null,
            inspectionNote: null,
            classifiedAtUtc: null,
            classifiedBy: null);
    }

    /// <summary>
    /// Returnable quantity is good-received minus already-returned (finalized plus active batches).
    /// Financial return value uses the locked unit purchase cost of the purchase-order line.
    /// </summary>
    internal static ReturnBatchLine CreateAcceptedFromPurchaseOrderLine(
        ReturnBatchId returnBatchId,
        PosOrganizationId sellerOrganizationId,
        PurchaseOrderLine purchaseOrderLine,
        ReturnBatchConnectedPoLineDraft draft,
        decimal previouslyReturnedQuantity)
    {
        if (purchaseOrderLine.Id != draft.PurchaseOrderLineId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchLine,
                "Return batch line must reference a line from the originating purchase order.");
        }

        if (purchaseOrderLine.ProductId is null || purchaseOrderLine.UomSnapshot is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchLine,
                "Purchase-order line must have a buyer product and unit of measure before it can be returned.");
        }

        var uom = purchaseOrderLine.UomSnapshot.Value;
        var acceptedQuantity = NormalizeQuantity(draft.AcceptedQuantity, uom);
        if (acceptedQuantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchLine,
                "Return quantity must be greater than zero.");
        }

        var prior = previouslyReturnedQuantity < 0m ? 0m : previouslyReturnedQuantity;
        var returnableQuantity = purchaseOrderLine.ReceivedQty - prior;
        if (returnableQuantity <= 0m || acceptedQuantity > returnableQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.ReturnBatchQuantityExceedsReceived,
                "Return quantity exceeds the good received quantity still available to return.");
        }

        var refundAmount = SaleMoney.RoundMoney(acceptedQuantity * purchaseOrderLine.UnitPurchaseCost);
        if (refundAmount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchAcceptedValue,
                "Accepted return value must be greater than zero.");
        }

        return new ReturnBatchLine(
            ReturnBatchLineId.New(),
            returnBatchId,
            sellerOrganizationId,
            saleLineId: null,
            purchaseOrderLine.ProductId,
            purchaseOrderLine.NameSnapshot ?? "Returned item",
            uom,
            purchaseOrderLine.UnitPurchaseCost,
            purchaseOrderLine.LineTotal,
            acceptedQuantity,
            refundAmount,
            sellableQuantity: null,
            damagedQuantity: null,
            inspectionNote: null,
            classifiedAtUtc: null,
            classifiedBy: null,
            purchaseOrderLine.Id,
            purchaseOrderLine.SupplierProductId);
    }

    public void Classify(
        decimal sellableQuantity,
        decimal damagedQuantity,
        string? inspectionNote,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        var normalizedSellable = NormalizeClassifiedQuantity(sellableQuantity);
        var normalizedDamaged = NormalizeClassifiedQuantity(damagedQuantity);
        if (normalizedSellable + normalizedDamaged != AcceptedQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchClassification,
                "Sellable and damaged quantities must add up to accepted quantity.");
        }

        SellableQuantity = normalizedSellable;
        DamagedQuantity = normalizedDamaged;
        InspectionNote = NormalizeInspectionNote(inspectionNote);
        ClassifiedAtUtc = utcNow;
        ClassifiedBy = actorId;
    }

    public static ReturnBatchLine Rehydrate(
        ReturnBatchLineId id,
        ReturnBatchId returnBatchId,
        PosOrganizationId organizationId,
        SaleLineId? saleLineId,
        CatalogProductId productId,
        string productNameSnapshot,
        UnitOfMeasure uomSnapshot,
        decimal unitPriceSnapshot,
        decimal lineTotalSnapshot,
        decimal acceptedQuantity,
        decimal refundAmountSnapshot,
        decimal? sellableQuantity,
        decimal? damagedQuantity,
        string? inspectionNote,
        DateTimeOffset? classifiedAtUtc,
        Guid? classifiedBy,
        PurchaseOrderLineId? purchaseOrderLineId = null,
        CatalogProductId? supplierProductId = null) =>
        new(
            id,
            returnBatchId,
            organizationId,
            saleLineId,
            productId,
            productNameSnapshot,
            uomSnapshot,
            unitPriceSnapshot,
            lineTotalSnapshot,
            acceptedQuantity,
            refundAmountSnapshot,
            sellableQuantity,
            damagedQuantity,
            inspectionNote,
            classifiedAtUtc,
            classifiedBy,
            purchaseOrderLineId,
            supplierProductId);

    private decimal NormalizeClassifiedQuantity(decimal quantity) => NormalizeQuantity(quantity, UomSnapshot);

    private static decimal NormalizeQuantity(decimal quantity, UnitOfMeasure uom)
    {
        if (quantity < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchClassification,
                "Classified quantities must be zero or greater.");
        }

        var maxDecimals = SaleMoney.MaxQuantityDecimals(uom);
        if (!SaleMoney.HasAtMostDecimals(quantity, maxDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchClassification,
                maxDecimals == 0
                    ? $"{uom} quantities must be whole numbers."
                    : $"{uom} quantities may have at most {maxDecimals} decimal places.");
        }

        return quantity;
    }

    private static string? NormalizeInspectionNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length > ReturnBatch.InspectionNoteMaxLength
            ? trimmed[..ReturnBatch.InspectionNoteMaxLength]
            : trimmed;
    }
}
