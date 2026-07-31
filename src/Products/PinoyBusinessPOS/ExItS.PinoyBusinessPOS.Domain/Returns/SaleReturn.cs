using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>
/// Organization-owned sale return recorded atomically as Completed. Immutable after create;
/// partial and multiple returns are allowed until refundable quantity is exhausted.
/// Refund method must match the originating sale payment method.
/// </summary>
public sealed class SaleReturn
{
    public const int ReasonMaxLength = 512;
    public const int NotesMaxLength = 512;
    public const int MaxLineCount = Sale.MaxLineCount;
    public const decimal MaxTotal = Sale.MaxTotal;

    private readonly List<SaleReturnLine> _lines;

    public SaleReturnId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string ReturnNumber { get; }
    public SaleId SaleId { get; }
    public CashierShiftId? CashierShiftId { get; }
    /// <summary>Register from the original sale; null for legacy unassigned sales.</summary>
    public RegisterId? SourceRegisterId { get; }
    /// <summary>Register from the cash-refund Open shift when applicable; otherwise null.</summary>
    public RegisterId? RefundRegisterId { get; }
    public SalePaymentMethod RefundMethod { get; }
    public SaleReturnStatus Status { get; }
    public DateOnly ReturnDate { get; }
    public string Reason { get; }
    public string? Notes { get; }
    public decimal TotalRefundAmount { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset CompletedAtUtc { get; }

    public IReadOnlyList<SaleReturnLine> Lines => _lines;

    private SaleReturn(
        SaleReturnId id,
        PosOrganizationId organizationId,
        string returnNumber,
        SaleId saleId,
        CashierShiftId? cashierShiftId,
        RegisterId? sourceRegisterId,
        RegisterId? refundRegisterId,
        SalePaymentMethod refundMethod,
        SaleReturnStatus status,
        DateOnly returnDate,
        string reason,
        string? notes,
        decimal totalRefundAmount,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset completedAtUtc,
        List<SaleReturnLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        ReturnNumber = returnNumber;
        SaleId = saleId;
        CashierShiftId = cashierShiftId;
        SourceRegisterId = sourceRegisterId;
        RefundRegisterId = refundRegisterId;
        RefundMethod = refundMethod;
        Status = status;
        ReturnDate = returnDate;
        Reason = reason;
        Notes = notes;
        TotalRefundAmount = totalRefundAmount;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
        CompletedAtUtc = completedAtUtc;
        _lines = lines;
    }

    /// <summary>
    /// Creates a completed return from validated line drafts against a completed sale.
    /// Prior returned quantities and refund amounts per sale line must be supplied by the caller.
    /// </summary>
    public static SaleReturn CreateCompleted(
        PosOrganizationId organizationId,
        string returnNumber,
        Sale sale,
        IReadOnlyList<SaleReturnLineDraft> lineDrafts,
        IReadOnlyDictionary<Guid, (decimal ReturnedQuantity, decimal RefundedAmount)> priorBySaleLineId,
        string reason,
        Guid createdBy,
        DateTimeOffset utcNow,
        CashierShiftId? cashierShiftId = null,
        RegisterId? refundRegisterId = null,
        DateOnly? returnDate = null,
        string? notes = null,
        SaleReturnId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdBy);

        if (sale.OrganizationId != organizationId)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnOrganizationMismatch,
                "Return must belong to the same organization as the sale.");
        }

        if (sale.Status != SaleStatus.Completed)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnSaleNotReturnable,
                "Only completed sales can be returned.");
        }

        if (lineDrafts is null || lineDrafts.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnRequiresAtLeastOneLine,
                "A return must contain at least one line.");
        }

        if (lineDrafts.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnRequiresAtLeastOneLine,
                $"A return may contain at most {MaxLineCount} lines.");
        }

        var refundMethod = sale.PaymentMethod;
        ValidateCashShift(refundMethod, cashierShiftId);

        var saleLinesById = sale.Lines.ToDictionary(l => l.Id.Value);
        var seenSaleLines = new HashSet<Guid>();
        var consolidated = ConsolidateLineDrafts(lineDrafts);

        var returnId = id ?? SaleReturnId.New();
        var lines = new List<SaleReturnLine>(consolidated.Count);
        foreach (var draft in consolidated)
        {
            if (!seenSaleLines.Add(draft.SaleLineId.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.SaleReturnDuplicateSaleLine,
                    "Duplicate sale line references are not allowed on a return.");
            }

            if (!saleLinesById.TryGetValue(draft.SaleLineId.Value, out var saleLine))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidSaleReturnLine,
                    "Return line must reference a line from the originating sale.");
            }

            priorBySaleLineId.TryGetValue(draft.SaleLineId.Value, out var prior);
            lines.Add(SaleReturnLine.Create(
                returnId,
                organizationId,
                saleLine,
                draft,
                prior.ReturnedQuantity,
                prior.RefundedAmount));
        }

        var total = SaleMoney.RoundMoney(lines.Sum(l => l.RefundAmount));
        if (total <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnRefundAmount,
                "Total refund amount must be greater than zero.");
        }

        if (total > MaxTotal)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnRefundAmount,
                "The return total is too large.");
        }

        var normalizedNumber = ReturnNumbers.Normalize(returnNumber);
        var normalizedReason = NormalizeReason(reason);
        var normalizedNotes = NormalizeNotes(notes);
        var businessDate = returnDate ?? ReturnNumbers.BusinessDateOf(utcNow);

        return new SaleReturn(
            returnId,
            organizationId,
            normalizedNumber,
            sale.Id,
            cashierShiftId,
            sale.RegisterId,
            refundRegisterId,
            refundMethod,
            SaleReturnStatus.Completed,
            businessDate,
            normalizedReason,
            normalizedNotes,
            total,
            utcNow,
            createdBy,
            utcNow,
            lines);
    }

    public static SaleReturn Rehydrate(
        SaleReturnId id,
        PosOrganizationId organizationId,
        string returnNumber,
        SaleId saleId,
        CashierShiftId? cashierShiftId,
        RegisterId? sourceRegisterId,
        RegisterId? refundRegisterId,
        SalePaymentMethod refundMethod,
        SaleReturnStatus status,
        DateOnly returnDate,
        string reason,
        string? notes,
        decimal totalRefundAmount,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset completedAtUtc,
        IEnumerable<SaleReturnLine> lines) =>
        new(
            id,
            organizationId,
            returnNumber,
            saleId,
            cashierShiftId,
            sourceRegisterId,
            refundRegisterId,
            refundMethod,
            status,
            returnDate,
            reason,
            notes,
            totalRefundAmount,
            createdAtUtc,
            createdBy,
            completedAtUtc,
            lines.ToList());

    private static void ValidateCashShift(SalePaymentMethod refundMethod, CashierShiftId? cashierShiftId)
    {
        if (refundMethod == SalePaymentMethod.Cash && cashierShiftId is null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnCashShiftRequired,
                "Cash refunds require an open cashier shift.");
        }

        if (refundMethod != SalePaymentMethod.Cash && cashierShiftId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnNonCashMustNotLinkShift,
                "Only cash refunds may link a cashier shift.");
        }
    }

    /// <summary>Folds duplicate sale-line references by summing quantities (deterministic order).</summary>
    private static List<SaleReturnLineDraft> ConsolidateLineDrafts(IReadOnlyList<SaleReturnLineDraft> drafts)
    {
        var order = new List<Guid>();
        var bySaleLine = new Dictionary<Guid, (decimal Quantity, RestockDisposition Disposition, string? LineReason)>();

        foreach (var draft in drafts.OrderBy(d => d.SaleLineId.Value))
        {
            if (!bySaleLine.TryGetValue(draft.SaleLineId.Value, out var running))
            {
                order.Add(draft.SaleLineId.Value);
                running = (0m, draft.RestockDisposition, draft.LineReason);
            }

            if (running.Disposition != draft.RestockDisposition)
            {
                throw new DomainException(
                    DomainErrorCodes.SaleReturnDuplicateSaleLine,
                    "Duplicate sale line references must use the same restock disposition.");
            }

            bySaleLine[draft.SaleLineId.Value] = (
                running.Quantity + draft.QuantityReturned,
                draft.RestockDisposition,
                draft.LineReason ?? running.LineReason);
        }

        return order
            .Select(id =>
            {
                var (qty, disposition, lineReason) = bySaleLine[id];
                return new SaleReturnLineDraft(
                    SaleLineId.From(id),
                    qty,
                    disposition,
                    lineReason);
            })
            .ToList();
    }

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnReason,
                "A return reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnReason,
                $"Return reason must be at most {ReasonMaxLength} characters.");
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
}
