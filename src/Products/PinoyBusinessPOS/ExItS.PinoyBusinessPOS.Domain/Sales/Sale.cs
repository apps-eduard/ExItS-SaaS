using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;

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

    /// <summary>Sales tax amount recorded at checkout. Zero for legacy sales and when tax is not configured.</summary>
    public decimal TaxAmount { get; }

    /// <summary>Cash tendered by the customer. Always null for ManualGCash and Utang.</summary>
    public decimal? AmountTendered { get; }

    /// <summary>Change owed back to the customer. Always null for ManualGCash and Utang.</summary>
    public decimal? ChangeAmount { get; }

    /// <summary>
    /// Optional manually typed GCash reference. Never verified against any gateway or GCash API.
    /// </summary>
    public string? GCashReference { get; private set; }

    /// <summary>
    /// Optional seller-owned customer association (ledger / local profile). Required for Product-Based Utang.
    /// Never determines sale ownership — see <see cref="BuyerParty"/>.
    /// </summary>
    public POSCustomerId? CustomerId { get; }

    /// <summary>
    /// Buyer/counterparty snapshot. Seller <see cref="OrganizationId"/> remains the transaction owner.
    /// </summary>
    public SaleBuyerParty BuyerParty { get; }

    /// <summary>Linked credit entry for Product-Based Utang only; null for settled payment methods.</summary>
    public CreditEntryId? LinkedCreditEntryId { get; }

    /// <summary>Open cashier shift at checkout; null for legacy pre-migration sales.</summary>
    public CashierShiftId? CashierShiftId { get; }

    /// <summary>Register inherited from the open shift; null for legacy pre-migration sales.</summary>
    public RegisterId? RegisterId { get; }

    /// <summary>
    /// Authoritative walk-in fulfillment/stock branch. Null only for legacy sales recorded before
    /// P28-WP13. New checkouts persist the validated operational branch.
    /// </summary>
    public PosBranchId? BranchId { get; }

    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }

    /// <summary>Last write timestamp used for optimistic concurrency checks by callers.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Inventory hold for provider-backed Card/GCash sales. Cash/Utang/ManualGCash remain
    /// <see cref="SaleStockReservationState.None"/>.
    /// </summary>
    public SaleStockReservationState StockReservationState { get; private set; }

    public IReadOnlyList<SaleLine> Lines => _lines;

    private Sale(
        SaleId id,
        PosOrganizationId organizationId,
        string saleNumber,
        SaleStatus status,
        SalePaymentMethod paymentMethod,
        decimal subtotal,
        decimal total,
        decimal taxAmount,
        decimal? amountTendered,
        decimal? changeAmount,
        string? gcashReference,
        POSCustomerId? customerId,
        SaleBuyerParty buyerParty,
        CreditEntryId? linkedCreditEntryId,
        CashierShiftId? cashierShiftId,
        RegisterId? registerId,
        PosBranchId? branchId,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? voidedAtUtc,
        Guid? voidedBy,
        string? voidReason,
        DateTimeOffset updatedAtUtc,
        List<SaleLine> lines,
        SaleStockReservationState stockReservationState)
    {
        Id = id;
        OrganizationId = organizationId;
        SaleNumber = saleNumber;
        Status = status;
        PaymentMethod = paymentMethod;
        Subtotal = subtotal;
        Total = total;
        TaxAmount = taxAmount;
        AmountTendered = amountTendered;
        ChangeAmount = changeAmount;
        GCashReference = gcashReference;
        CustomerId = customerId;
        BuyerParty = buyerParty;
        LinkedCreditEntryId = linkedCreditEntryId;
        CashierShiftId = cashierShiftId;
        RegisterId = registerId;
        BranchId = branchId;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
        VoidedAtUtc = voidedAtUtc;
        VoidedBy = voidedBy;
        VoidReason = voidReason;
        UpdatedAtUtc = updatedAtUtc;
        StockReservationState = stockReservationState;
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
        CreditEntryId? linkedCreditEntryId = null,
        CashierShiftId? cashierShiftId = null,
        RegisterId? registerId = null,
        decimal taxAmount = 0,
        TaxPricingMode? taxPricingMode = null,
        SaleBuyerParty? buyerParty = null,
        PosBranchId? branchId = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(recordedBy);

        if (cashierShiftId is null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleCashierShiftRequired,
                "Checkout requires an open cashier shift.");
        }

        if (registerId is null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleRegisterRequired,
                "Checkout requires a register inherited from the open shift.");
        }

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

        var normalizedTax = SaleMoney.RoundMoney(Math.Max(0m, taxAmount));
        var total = subtotal;
        if (normalizedTax > 0)
        {
            total = taxPricingMode switch
            {
                TaxPricingMode.TaxExclusive => subtotal + normalizedTax,
                TaxPricingMode.TaxInclusive => subtotal,
                _ => subtotal
            };
            total = SaleMoney.RoundMoney(total);
        }

        if (total > MaxTotal)
        {
            throw new DomainException(DomainErrorCodes.SaleTotalTooLarge, "The sale total is too large.");
        }

        ValidatePaymentLinkage(paymentMethod, customerId, linkedCreditEntryId, total);

        var resolvedBuyer = buyerParty ?? SaleBuyerParty.FromLegacyCustomer(customerId);
        resolvedBuyer.EnsureConsistentWith(customerId);

        var (tendered, change) = NormalizeTender(paymentMethod, total, amountTendered);
        var reference = NormalizeGCashReference(paymentMethod, gcashReference);
        var initialStatus = SalePaymentMethods.IsElectronic(paymentMethod)
            ? SaleStatus.AwaitingPayment
            : SaleStatus.Completed;

        return new Sale(
            saleId,
            organizationId,
            normalizedNumber,
            initialStatus,
            paymentMethod,
            subtotal,
            total,
            normalizedTax,
            tendered,
            change,
            reference,
            customerId,
            resolvedBuyer,
            linkedCreditEntryId,
            cashierShiftId,
            registerId,
            branchId,
            utcNow,
            recordedBy,
            null,
            null,
            null,
            utcNow,
            saleLines,
            SaleStockReservationState.None);
    }

    /// <summary>
    /// Completes an electronic sale after an authoritative Paid payment attempt.
    /// Idempotent when already Completed with the same payment method.
    /// </summary>
    public void FinalizeAfterPayment(string? providerSafeReference, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status == SaleStatus.Completed)
        {
            return;
        }

        if (Status == SaleStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleStatusTransition,
                "A voided sale cannot be finalized.");
        }

        if (Status != SaleStatus.AwaitingPayment)
        {
            throw new DomainException(
                DomainErrorCodes.SaleNotAwaitingPayment,
                "Only sales awaiting payment can be finalized from a payment attempt.");
        }

        if (!SalePaymentMethods.IsElectronic(PaymentMethod)
            && PaymentMethod != SalePaymentMethod.ManualGCash)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSalePaymentMethod,
                "Finalize-after-payment applies to Card, GCash, or Manual GCash sales.");
        }

        if (providerSafeReference is not null)
        {
            var trimmed = providerSafeReference.Trim();
            if (trimmed.Length > GCashReferenceMaxLength)
            {
                trimmed = trimmed[..GCashReferenceMaxLength];
            }

            GCashReference = trimmed;
        }

        Status = SaleStatus.Completed;
        UpdatedAtUtc = utcNow;
    }

    public static Sale Rehydrate(
        SaleId id,
        PosOrganizationId organizationId,
        string saleNumber,
        SaleStatus status,
        SalePaymentMethod paymentMethod,
        decimal subtotal,
        decimal total,
        decimal taxAmount,
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
        CreditEntryId? linkedCreditEntryId = null,
        CashierShiftId? cashierShiftId = null,
        RegisterId? registerId = null,
        SaleBuyerParty? buyerParty = null,
        SaleStockReservationState stockReservationState = SaleStockReservationState.None,
        PosBranchId? branchId = null) =>
        new(
            id,
            organizationId,
            saleNumber,
            status,
            paymentMethod,
            subtotal,
            total,
            taxAmount,
            amountTendered,
            changeAmount,
            gcashReference,
            customerId,
            buyerParty ?? SaleBuyerParty.FromLegacyCustomer(customerId),
            linkedCreditEntryId,
            cashierShiftId,
            registerId,
            branchId,
            recordedAtUtc,
            recordedBy,
            voidedAtUtc,
            voidedBy,
            voidReason,
            updatedAtUtc,
            lines.OrderBy(l => l.LineNumber).ToList(),
            stockReservationState);

    /// <summary>
    /// Marks inventory as reserved for an electronic sale awaiting payment.
    /// Idempotent when already Reserved; Released may re-reserve for payment retry.
    /// </summary>
    public void MarkStockReserved(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (StockReservationState == SaleStockReservationState.Reserved)
        {
            return;
        }

        if (StockReservationState is not (SaleStockReservationState.None or SaleStockReservationState.Released))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleStockReservation,
                "Stock can only be reserved from None or Released.");
        }

        StockReservationState = SaleStockReservationState.Reserved;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Releases a prior reservation (decline, cancel, expire, void while awaiting payment).
    /// Idempotent when already Released.
    /// </summary>
    public void MarkStockReleased(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (StockReservationState == SaleStockReservationState.Released)
        {
            return;
        }

        if (StockReservationState != SaleStockReservationState.Reserved)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleStockReservation,
                "Stock can only be released from the Reserved state.");
        }

        StockReservationState = SaleStockReservationState.Released;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Converts a reservation into a consumed deduction after Paid finalization.
    /// Idempotent when already Consumed. Allows Released→Consumed when an authoritative
    /// Paid arrives after a local release (provider wins; stock deducted via fallback path).
    /// </summary>
    public void MarkStockConsumed(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (StockReservationState == SaleStockReservationState.Consumed)
        {
            return;
        }

        if (StockReservationState is not (SaleStockReservationState.Reserved or SaleStockReservationState.Released))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleStockReservation,
                "Stock can only be consumed from the Reserved or Released state.");
        }

        StockReservationState = SaleStockReservationState.Consumed;
        UpdatedAtUtc = utcNow;
    }

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
        if (paymentMethod is SalePaymentMethod.ManualGCash
            or SalePaymentMethod.Utang
            or SalePaymentMethod.Card
            or SalePaymentMethod.GCash)
        {
            if (amountTendered is not null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidSaleAmountTendered,
                    paymentMethod switch
                    {
                        SalePaymentMethod.Utang =>
                            "Utang sales are recorded for the exact total and must not carry a tendered amount.",
                        SalePaymentMethod.Card or SalePaymentMethod.GCash =>
                            "Card and GCash sales are recorded for the exact total and must not carry a tendered amount.",
                        _ => "Manual GCash sales are recorded for the exact total and must not carry a tendered amount."
                    });
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

        // Settled payments may optionally attach a customer (linked-merchant receipts/activity).
        // They must never attach a credit entry / Utang linkage.
        if (linkedCreditEntryId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleCashMustNotLinkCredit,
                "Cash, Card, GCash, and Manual GCash sales must not link a credit entry.");
        }
    }
}
