using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Organization-owned simple retail sale. A sale is recorded complete in one checkout and is
/// immutable afterwards: the only permitted transition is an explicit void with a reason and actor.
///
/// Money handling: line totals and sale totals are rounded to two decimal places with
/// <see cref="MidpointRounding.AwayFromZero"/> (see <see cref="SaleMoney"/>), matching the
/// <c>CreditEntry</c>/<c>Repayment</c> convention so peso amounts reconcile across the product.
///
/// Payment methods: Cash, ManualGCash, and Product-Based Utang (linked remarks credit). Out of scope
/// by design: stock/inventory deduction, split or partial tender, discounts, tax/VAT, fees, tips,
/// refunds/returns/exchanges, line voids, fiscal invoices, payment gateways, GCash verification,
/// credit limits, and offline sale capture.
/// </summary>
public sealed class Sale
{
    public const int VoidReasonMaxLength = 512;
    public const int GCashReferenceMaxLength = 64;
    public const int MaxLineCount = 200;
    public const decimal MaxTotal = 999_999_999.99m;

    private readonly List<SaleLine> _lines;

    public SaleId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string SaleNumber { get; }
    public SaleStatus Status { get; private set; }
    public SalePaymentMethod PaymentMethod { get; }
    public decimal Subtotal { get; }
    public decimal Total { get; }

    /// <summary>Cash tendered by the customer. Always null for ManualGCash and Utang.</summary>
    public decimal? AmountTendered { get; }

    /// <summary>Change owed back to the customer. Always null for ManualGCash and Utang.</summary>
    public decimal? ChangeAmount { get; }

    /// <summary>
    /// Optional manually typed GCash reference. Never verified against any gateway or GCash API.
    /// </summary>
    public string? GCashReference { get; }

    /// <summary>Customer for Product-Based Utang; null for Cash and ManualGCash.</summary>
    public POSCustomerId? CustomerId { get; }

    /// <summary>Linked credit entry for Product-Based Utang; null for Cash and ManualGCash.</summary>
    public CreditEntryId? LinkedCreditEntryId { get; }

    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }

    /// <summary>Last write timestamp used for optimistic concurrency checks by callers.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<SaleLine> Lines => _lines;

    private Sale(
        SaleId id,
        PosOrganizationId organizationId,
        string saleNumber,
        SaleStatus status,
        SalePaymentMethod paymentMethod,
        decimal subtotal,
        decimal total,
        decimal? amountTendered,
        decimal? changeAmount,
        string? gcashReference,
        POSCustomerId? customerId,
        CreditEntryId? linkedCreditEntryId,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? voidedAtUtc,
        Guid? voidedBy,
        string? voidReason,
        DateTimeOffset updatedAtUtc,
        List<SaleLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        SaleNumber = saleNumber;
        Status = status;
        PaymentMethod = paymentMethod;
        Subtotal = subtotal;
        Total = total;
        AmountTendered = amountTendered;
        ChangeAmount = changeAmount;
        GCashReference = gcashReference;
        CustomerId = customerId;
        LinkedCreditEntryId = linkedCreditEntryId;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
        VoidedAtUtc = voidedAtUtc;
        VoidedBy = voidedBy;
        VoidReason = voidReason;
        UpdatedAtUtc = updatedAtUtc;
        _lines = lines;
    }

    /// <summary>
    /// Records a completed sale from validated snapshot line drafts. The sale number is allocated
    /// server-side before this call; clients never supply one.
    /// </summary>
    public static Sale Checkout(
        PosOrganizationId organizationId,
        string saleNumber,
        SalePaymentMethod paymentMethod,
        IReadOnlyList<SaleLineDraft> lines,
        Guid recordedBy,
        DateTimeOffset utcNow,
        decimal? amountTendered = null,
        string? gcashReference = null,
        SaleId? id = null,
        POSCustomerId? customerId = null,
        CreditEntryId? linkedCreditEntryId = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(recordedBy);

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.SaleRequiresAtLeastOneLine,
                "A sale must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.SaleRequiresAtLeastOneLine,
                $"A sale may contain at most {MaxLineCount} lines.");
        }

        var saleId = id ?? SaleId.New();
        var normalizedNumber = SaleNumbers.Normalize(saleNumber);

        var saleLines = new List<SaleLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            saleLines.Add(SaleLine.Create(saleId, organizationId, i + 1, lines[i]));
        }

        var subtotal = SaleMoney.RoundMoney(saleLines.Sum(l => l.LineTotal));
        if (subtotal > MaxTotal)
        {
            throw new DomainException(DomainErrorCodes.SaleTotalTooLarge, "The sale total is too large.");
        }

        // No tax, discount, fee, or tip adjustments exist in this scope, so total equals subtotal.
        var total = subtotal;

        ValidatePaymentLinkage(paymentMethod, customerId, linkedCreditEntryId, total);

        var (tendered, change) = NormalizeTender(paymentMethod, total, amountTendered);
        var reference = NormalizeGCashReference(paymentMethod, gcashReference);

        return new Sale(
            saleId,
            organizationId,
            normalizedNumber,
            SaleStatus.Completed,
            paymentMethod,
            subtotal,
            total,
            tendered,
            change,
            reference,
            customerId,
            linkedCreditEntryId,
            utcNow,
            recordedBy,
            null,
            null,
            null,
            utcNow,
            saleLines);
    }

    public static Sale Rehydrate(
        SaleId id,
        PosOrganizationId organizationId,
        string saleNumber,
        SaleStatus status,
        SalePaymentMethod paymentMethod,
        decimal subtotal,
        decimal total,
        decimal? amountTendered,
        decimal? changeAmount,
        string? gcashReference,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? voidedAtUtc,
        Guid? voidedBy,
        string? voidReason,
        DateTimeOffset updatedAtUtc,
        IEnumerable<SaleLine> lines,
        POSCustomerId? customerId = null,
        CreditEntryId? linkedCreditEntryId = null) =>
        new(
            id,
            organizationId,
            saleNumber,
            status,
            paymentMethod,
            subtotal,
            total,
            amountTendered,
            changeAmount,
            gcashReference,
            customerId,
            linkedCreditEntryId,
            recordedAtUtc,
            recordedBy,
            voidedAtUtc,
            voidedBy,
            voidReason,
            updatedAtUtc,
            lines.OrderBy(l => l.LineNumber).ToList());

    /// <summary>
    /// Voids a completed sale. Voiding is the only correction available: it does not refund money
    /// or return stock. For Utang sales the application layer reverses the linked credit in the same
    /// transaction; this domain method only marks the sale voided. Only Completed to Voided is permitted.
    /// </summary>
    public void Void(string reason, Guid voidedBy, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(voidedBy);

        if (Status == SaleStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleStatusTransition,
                "Sale is already voided.");
        }

        // Validate everything before mutating so a rejected void leaves the sale fully intact.
        var normalizedReason = NormalizeVoidReason(reason);

        Status = SaleStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedBy = voidedBy;
        VoidReason = normalizedReason;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Cash sales require a tender at least equal to the total and derive change from it.
    /// Manual GCash and Utang sales are recorded for the exact total, so tender and change stay null.
    /// </summary>
    public static (decimal? AmountTendered, decimal? ChangeAmount) NormalizeTender(
        SalePaymentMethod paymentMethod,
        decimal total,
        decimal? amountTendered)
    {
        if (paymentMethod is SalePaymentMethod.ManualGCash or SalePaymentMethod.Utang)
        {
            if (amountTendered is not null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidSaleAmountTendered,
                    paymentMethod == SalePaymentMethod.Utang
                        ? "Utang sales are recorded for the exact total and must not carry a tendered amount."
                        : "Manual GCash sales are recorded for the exact total and must not carry a tendered amount.");
            }

            return (null, null);
        }

        if (amountTendered is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleAmountTendered,
                "Cash sales require the amount tendered.");
        }

        var tendered = amountTendered.Value;
        if (tendered < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleAmountTendered,
                "Amount tendered cannot be negative.");
        }

        if (!SaleMoney.HasAtMostDecimals(tendered, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleAmountTendered,
                "Amount tendered must have at most 2 decimal places.");
        }

        if (tendered > MaxTotal)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleAmountTendered,
                "Amount tendered is too large.");
        }

        if (tendered < total)
        {
            throw new DomainException(
                DomainErrorCodes.SaleAmountTenderedBelowTotal,
                "Amount tendered must be at least the sale total.");
        }

        return (tendered, SaleMoney.RoundMoney(tendered - total));
    }

    /// <summary>
    /// Trims an optional manual GCash reference. The reference is operator-typed evidence only and
    /// is never validated against GCash.
    /// </summary>
    public static string? NormalizeGCashReference(SalePaymentMethod paymentMethod, string? gcashReference)
    {
        if (string.IsNullOrWhiteSpace(gcashReference))
        {
            return null;
        }

        if (paymentMethod != SalePaymentMethod.ManualGCash)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleGCashReference,
                "A GCash reference can only be recorded on a manual GCash sale.");
        }

        var trimmed = gcashReference.Trim();
        if (trimmed.Length > GCashReferenceMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleGCashReference,
                $"GCash reference must be at most {GCashReferenceMaxLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeVoidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleVoidReason,
                "A void reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > VoidReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleVoidReason,
                $"Void reason must be at most {VoidReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static void ValidatePaymentLinkage(
        SalePaymentMethod paymentMethod,
        POSCustomerId? customerId,
        CreditEntryId? linkedCreditEntryId,
        decimal total)
    {
        if (paymentMethod == SalePaymentMethod.Utang)
        {
            if (customerId is null)
            {
                throw new DomainException(
                    DomainErrorCodes.SaleUtangCustomerRequired,
                    "Product-Based Utang requires a customer.");
            }

            if (linkedCreditEntryId is null)
            {
                throw new DomainException(
                    DomainErrorCodes.SaleUtangLinkageInvalid,
                    "Product-Based Utang requires a linked credit entry id.");
            }

            if (total <= 0m)
            {
                throw new DomainException(
                    DomainErrorCodes.SaleUtangTotalMustBePositive,
                    "Product-Based Utang total must be greater than zero.");
            }

            return;
        }

        if (customerId is not null || linkedCreditEntryId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleCashMustNotLinkCredit,
                "Cash and Manual GCash sales must not link a customer or credit entry.");
        }
    }
}
