using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
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
/// by design: stock/inventory deduction, split or partial tender, fees, tips,
/// refunds/returns/exchanges, line voids, fiscal invoices, payment gateways, GCash verification,
/// credit limits, and offline sale capture.
///
/// Commercial discounts (RMAP-B03) are additive and money-only: <see cref="GrossSubtotal"/> keeps the
/// pre-discount amount, <see cref="Subtotal"/> stays the net pre-tax base, and tax is computed from
/// the net subtotal. Sale price overrides (RMAP-B01) change line <see cref="SaleLine.UnitPrice"/>
/// only for the recorded sale — never catalog SellingPrice / Today's Price — and run before
/// commercial discounts. Promotions and statutory/regulatory discounts remain separate concepts.
/// </summary>
public sealed class Sale
{
    public const int VoidReasonMaxLength = 512;
    public const int GCashReferenceMaxLength = 64;
    public const int MaxLineCount = 200;
    public const decimal MaxTotal = 999_999_999.99m;

    private readonly List<SaleLine> _lines;
    private readonly List<SaleCommercialDiscountAdjustment> _commercialDiscounts;
    private readonly List<SalePriceOverrideAdjustment> _priceOverrides;

    public SaleId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string SaleNumber { get; }
    public SaleStatus Status { get; private set; }
    public SalePaymentMethod PaymentMethod { get; }

    /// <summary>Net pre-tax subtotal (after commercial discounts). The tax base and DTO contract.</summary>
    public decimal Subtotal { get; }

    public decimal Total { get; }

    /// <summary>Pre-discount subtotal: the sum of gross line totals. Equals Subtotal when undiscounted.</summary>
    public decimal GrossSubtotal { get; }

    /// <summary>Sum of commercial discounts applied directly to individual lines.</summary>
    public decimal LineDiscountTotal { get; }

    /// <summary>Sum of sale-level commercial discount allocated across lines.</summary>
    public decimal SaleDiscountTotal { get; }

    /// <summary>Total commercial discount taken off this sale.</summary>
    public decimal DiscountTotal { get; }

    /// <summary>
    /// COGS completeness for snapshotted line costs. Legacy sales without stored cost status are
    /// rehydrated as <see cref="ProductionCostStatus.Unavailable"/>.
    /// </summary>
    public ProductionCostStatus CostStatus { get; }

    /// <summary>
    /// Sum of known line COGS when <see cref="CostStatus"/> is Partial or Complete; null when Unavailable.
    /// When Complete, equals full sale COGS at checkout.
    /// </summary>
    public decimal? TotalCostSnapshot { get; }

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

    /// <summary>
    /// Audit snapshots of the commercial discounts applied at checkout — one per requested intent.
    /// Empty for undiscounted and legacy sales.
    /// </summary>
    public IReadOnlyList<SaleCommercialDiscountAdjustment> CommercialDiscounts => _commercialDiscounts;

    /// <summary>
    /// Audit snapshots of per-sale unit-price overrides applied at checkout — one per requested intent.
    /// Empty when no override was applied (including legacy sales).
    /// </summary>
    public IReadOnlyList<SalePriceOverrideAdjustment> PriceOverrides => _priceOverrides;

    private Sale(
        SaleId id,
        PosOrganizationId organizationId,
        string saleNumber,
        SaleStatus status,
        SalePaymentMethod paymentMethod,
        decimal subtotal,
        decimal total,
        decimal taxAmount,
        decimal grossSubtotal,
        decimal lineDiscountTotal,
        decimal saleDiscountTotal,
        ProductionCostStatus costStatus,
        decimal? totalCostSnapshot,
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
        SaleStockReservationState stockReservationState,
        List<SaleCommercialDiscountAdjustment> commercialDiscounts,
        List<SalePriceOverrideAdjustment> priceOverrides)
    {
        Id = id;
        OrganizationId = organizationId;
        SaleNumber = saleNumber;
        Status = status;
        PaymentMethod = paymentMethod;
        Subtotal = subtotal;
        Total = total;
        TaxAmount = taxAmount;
        GrossSubtotal = grossSubtotal;
        LineDiscountTotal = lineDiscountTotal;
        SaleDiscountTotal = saleDiscountTotal;
        DiscountTotal = SaleMoney.RoundMoney(lineDiscountTotal + saleDiscountTotal);
        CostStatus = costStatus;
        TotalCostSnapshot = totalCostSnapshot;
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
        _commercialDiscounts = commercialDiscounts;
        _priceOverrides = priceOverrides;
    }

    /// <summary>
    /// Records a completed sale from validated snapshot line drafts. The sale number is allocated
    /// server-side before this call; clients never supply one.
    ///
    /// Order of money operations is fixed: apply optional per-sale unit-price overrides to draft
    /// UnitPrice → build lines → apply commercial discounts on GrossLineTotal. When
    /// <paramref name="commercialDiscounts"/> or <paramref name="priceOverrides"/> are supplied,
    /// this method recomputes every peso from the intents independently of anything the application
    /// layer already quoted. <paramref name="taxAmount"/> must already have been computed from the
    /// net (post-discount) subtotal. <paramref name="allowUnlimitedSalePriceOverride"/> selects the
    /// manager deviation ceiling versus Owner-unlimited positive prices.
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
        PosBranchId? branchId = null,
        IReadOnlyList<CommercialDiscountIntent>? commercialDiscounts = null,
        IReadOnlyList<SalePriceOverrideIntent>? priceOverrides = null,
        bool allowUnlimitedSalePriceOverride = false)
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

        EnsureLineCount(lines);

        var saleId = id ?? SaleId.New();
        var normalizedNumber = SaleNumbers.Normalize(saleNumber);

        var overrideResult = SalePriceOverrideApplier.Apply(
            lines,
            priceOverrides,
            allowUnlimitedSalePriceOverride
                ? null
                : SalePriceOverrideRules.ManagerMaxDeviationRatio);

        var saleLines = BuildLines(saleId, organizationId, overrideResult.Drafts);

        var grossSubtotal = SaleMoney.RoundMoney(saleLines.Sum(l => l.GrossLineTotal));
        if (grossSubtotal > MaxTotal)
        {
            throw new DomainException(DomainErrorCodes.SaleTotalTooLarge, "The sale total is too large.");
        }

        var discountResult = SaleCommercialDiscountCalculator.Apply(ToDiscountBases(saleLines), commercialDiscounts);

        foreach (var outcome in discountResult.Lines)
        {
            saleLines[outcome.LineNumber - 1].ApplyCommercialDiscount(outcome);
        }

        var discountAdjustments = new List<SaleCommercialDiscountAdjustment>(discountResult.Adjustments.Count);
        foreach (var draft in discountResult.Adjustments)
        {
            var lineId = draft.LineNumber is int lineNumber
                ? saleLines[lineNumber - 1].Id
                : null;

            discountAdjustments.Add(SaleCommercialDiscountAdjustment.Create(
                saleId,
                organizationId,
                draft,
                lineId,
                recordedBy,
                utcNow));
        }

        var priceOverrideAdjustments = new List<SalePriceOverrideAdjustment>(overrideResult.Adjustments.Count);
        foreach (var draft in overrideResult.Adjustments)
        {
            priceOverrideAdjustments.Add(SalePriceOverrideAdjustment.Create(
                saleId,
                organizationId,
                draft,
                saleLines[draft.LineNumber - 1].Id,
                recordedBy,
                utcNow));
        }

        var subtotal = SaleMoney.RoundMoney(saleLines.Sum(l => l.LineTotal));

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

        // A 100% commercial discount can legitimately drive the total to zero. Cash accepts that
        // (tender 0, change 0); Utang still requires a positive total because a zero-peso credit
        // entry would be meaningless. That existing rule is deliberately left in place.
        ValidatePaymentLinkage(paymentMethod, customerId, linkedCreditEntryId, total);

        var resolvedBuyer = buyerParty ?? SaleBuyerParty.FromLegacyCustomer(customerId);
        resolvedBuyer.EnsureConsistentWith(customerId);

        var (tendered, change) = NormalizeTender(paymentMethod, total, amountTendered);
        var reference = NormalizeGCashReference(paymentMethod, gcashReference);
        var initialStatus = SalePaymentMethods.IsElectronic(paymentMethod)
            ? SaleStatus.AwaitingPayment
            : SaleStatus.Completed;

        var (costStatus, totalCostSnapshot) = ComputeCostSnapshot(saleLines);

        return new Sale(
            saleId,
            organizationId,
            normalizedNumber,
            initialStatus,
            paymentMethod,
            subtotal,
            total,
            normalizedTax,
            grossSubtotal,
            discountResult.LineDiscountTotal,
            discountResult.SaleDiscountTotal,
            costStatus,
            totalCostSnapshot,
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
            SaleStockReservationState.None,
            discountAdjustments,
            priceOverrideAdjustments);
    }

    /// <summary>
    /// Records a completed customer-order settlement sale after inventory was consumed via the customer-order stock path.
    /// Accounting-only: <see cref="SaleStockReservationState.Consumed"/> prevents duplicate stock deduction.
    /// No cashier shift or register is required. <paramref name="authoritativeTotal"/> must match
    /// <see cref="CustomerOrder.Total"/> exactly.
    /// </summary>
    public static Sale RecordCustomerOrderSettlement(
        PosOrganizationId organizationId,
        string saleNumber,
        CustomerOrder sourceOrder,
        decimal authoritativeTotal,
        IReadOnlyList<SaleLineDraft> lines,
        Guid recordedBy,
        DateTimeOffset utcNow,
        SalePaymentMethod paymentMethod,
        POSCustomerId? customerId = null,
        CreditEntryId? linkedCreditEntryId = null,
        SaleBuyerParty? buyerParty = null,
        PosBranchId? branchId = null,
        SaleId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(recordedBy);
        ArgumentNullException.ThrowIfNull(sourceOrder);
        EnsureLineCount(lines);

        var saleId = id ?? SaleId.New();
        var normalizedNumber = SaleNumbers.Normalize(saleNumber);
        var saleLines = BuildLines(saleId, organizationId, lines);
        var grossSubtotal = SaleMoney.RoundMoney(saleLines.Sum(l => l.GrossLineTotal));
        var subtotal = SaleMoney.RoundMoney(saleLines.Sum(l => l.LineTotal));
        var total = subtotal;

        if (total != SaleMoney.RoundMoney(authoritativeTotal))
        {
            throw new DomainException(
                DomainErrorCodes.SaleTotalMismatch,
                "Customer order settlement total must match the authoritative order total.");
        }

        if (total > MaxTotal)
        {
            throw new DomainException(DomainErrorCodes.SaleTotalTooLarge, "The sale total is too large.");
        }

        ValidatePaymentLinkage(paymentMethod, customerId, linkedCreditEntryId, total);

        var resolvedBuyer = buyerParty
            ?? (customerId is not null
                ? SaleBuyerParty.FromLegacyCustomer(customerId)
                : SaleBuyerParty.ExternalCustomer(sourceOrder.CustomerParty.DisplayNameSnapshot));
        if (customerId is not null)
        {
            resolvedBuyer.EnsureConsistentWith(customerId);
        }

        var (amountTendered, changeAmount) = paymentMethod == SalePaymentMethod.Cash
            ? NormalizeTender(paymentMethod, total, total)
            : NormalizeTender(paymentMethod, total, null);

        var (costStatus, totalCostSnapshot) = ComputeCostSnapshot(saleLines);

        return new Sale(
            saleId,
            organizationId,
            normalizedNumber,
            SaleStatus.Completed,
            paymentMethod,
            subtotal,
            total,
            taxAmount: 0m,
            grossSubtotal,
            lineDiscountTotal: 0m,
            saleDiscountTotal: 0m,
            costStatus,
            totalCostSnapshot,
            amountTendered,
            changeAmount,
            gcashReference: null,
            customerId,
            resolvedBuyer,
            linkedCreditEntryId,
            cashierShiftId: null,
            registerId: null,
            branchId,
            utcNow,
            recordedBy,
            null,
            null,
            null,
            utcNow,
            saleLines,
            SaleStockReservationState.Consumed,
            [],
            []);
    }

    /// <summary>
    /// Records a completed Product-Based Utang sale for a fulfilled Personal customer order.
    /// Inventory was already consumed via the customer-order stock path; this sale is accounting-only.
    /// No cashier shift or register is required. <paramref name="authoritativeTotal"/> must match
    /// <see cref="CustomerOrder.Total"/> exactly.
    /// </summary>
    public static Sale RecordCustomerOrderUtangSettlement(
        PosOrganizationId organizationId,
        string saleNumber,
        CustomerOrder sourceOrder,
        decimal authoritativeTotal,
        IReadOnlyList<SaleLineDraft> lines,
        Guid recordedBy,
        DateTimeOffset utcNow,
        POSCustomerId customerId,
        CreditEntryId linkedCreditEntryId,
        SaleBuyerParty? buyerParty = null,
        PosBranchId? branchId = null,
        SaleId? id = null) =>
        RecordCustomerOrderSettlement(
            organizationId,
            saleNumber,
            sourceOrder,
            authoritativeTotal,
            lines,
            recordedBy,
            utcNow,
            SalePaymentMethod.Utang,
            customerId,
            linkedCreditEntryId,
            buyerParty ?? SaleBuyerParty.ExternalCustomer(sourceOrder.CustomerParty.DisplayNameSnapshot),
            branchId,
            id);

    /// <summary>
    /// Runs the exact line-building, price-override, and discount math <see cref="Checkout"/> would
    /// run, without creating or persisting a sale. Used by the quote endpoint so an operator can
    /// preview overrides and discounts before committing. A quote is advisory only: checkout
    /// recomputes from scratch and may legitimately reject or produce different numbers if prices
    /// or the cart changed.
    /// </summary>
    public static SaleQuoteMoneyResult QuoteCheckoutMoney(
        PosOrganizationId organizationId,
        IReadOnlyList<SaleLineDraft> lines,
        IReadOnlyList<CommercialDiscountIntent>? commercialDiscounts = null,
        IReadOnlyList<SalePriceOverrideIntent>? priceOverrides = null,
        bool allowUnlimitedSalePriceOverride = false)
    {
        EnsureLineCount(lines);

        var overrideResult = SalePriceOverrideApplier.Apply(
            lines,
            priceOverrides,
            allowUnlimitedSalePriceOverride
                ? null
                : SalePriceOverrideRules.ManagerMaxDeviationRatio);

        var saleLines = BuildLines(SaleId.New(), organizationId, overrideResult.Drafts);
        var grossSubtotal = SaleMoney.RoundMoney(saleLines.Sum(l => l.GrossLineTotal));
        if (grossSubtotal > MaxTotal)
        {
            throw new DomainException(DomainErrorCodes.SaleTotalTooLarge, "The sale total is too large.");
        }

        var discountResult = SaleCommercialDiscountCalculator.Apply(ToDiscountBases(saleLines), commercialDiscounts);
        return new SaleQuoteMoneyResult(overrideResult, discountResult, overrideResult.Drafts);
    }

    /// <summary>
    /// Discount-only quote used by callers that already applied price overrides to the drafts.
    /// Prefer <see cref="QuoteCheckoutMoney"/> when both override and discount intents are present.
    /// </summary>
    public static SaleCommercialDiscountResult QuoteCommercialDiscounts(
        PosOrganizationId organizationId,
        IReadOnlyList<SaleLineDraft> lines,
        IReadOnlyList<CommercialDiscountIntent>? commercialDiscounts = null)
    {
        EnsureLineCount(lines);

        var saleLines = BuildLines(SaleId.New(), organizationId, lines);
        var grossSubtotal = SaleMoney.RoundMoney(saleLines.Sum(l => l.GrossLineTotal));
        if (grossSubtotal > MaxTotal)
        {
            throw new DomainException(DomainErrorCodes.SaleTotalTooLarge, "The sale total is too large.");
        }

        return SaleCommercialDiscountCalculator.Apply(ToDiscountBases(saleLines), commercialDiscounts);
    }

    private static void EnsureLineCount(IReadOnlyList<SaleLineDraft>? lines)
    {
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
    }

    private static List<SaleLine> BuildLines(
        SaleId saleId,
        PosOrganizationId organizationId,
        IReadOnlyList<SaleLineDraft> lines)
    {
        var saleLines = new List<SaleLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            saleLines.Add(SaleLine.Create(saleId, organizationId, i + 1, lines[i]));
        }

        return saleLines;
    }

    private static SaleDiscountLineBasis[] ToDiscountBases(IReadOnlyList<SaleLine> saleLines) =>
        saleLines
            .Select(l => new SaleDiscountLineBasis(l.LineNumber, l.ProductId, l.GrossLineTotal))
            .ToArray();

    private static (ProductionCostStatus CostStatus, decimal? TotalCostSnapshot) ComputeCostSnapshot(
        IReadOnlyList<SaleLine> lines)
    {
        var costStatus = ProductionCostStatuses.FromMaterialCosts(lines.Select(l => l.UnitCostSnapshot).ToList());
        decimal? totalCost = costStatus == ProductionCostStatus.Unavailable
            ? null
            : SaleMoney.RoundMoney(lines.Where(l => l.LineCostSnapshot is not null).Sum(l => l.LineCostSnapshot!.Value));
        return (costStatus, totalCost);
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
        PosBranchId? branchId = null,
        decimal? grossSubtotal = null,
        decimal lineDiscountTotal = 0m,
        decimal saleDiscountTotal = 0m,
        IEnumerable<SaleCommercialDiscountAdjustment>? commercialDiscounts = null,
        IEnumerable<SalePriceOverrideAdjustment>? priceOverrides = null,
        ProductionCostStatus costStatus = ProductionCostStatus.Unavailable,
        decimal? totalCostSnapshot = null) =>
        new(
            id,
            organizationId,
            saleNumber,
            status,
            paymentMethod,
            subtotal,
            total,
            taxAmount,
            // Sales recorded before commercial discounts existed carry no gross subtotal:
            // their net subtotal is also their gross.
            grossSubtotal ?? subtotal,
            lineDiscountTotal,
            saleDiscountTotal,
            costStatus,
            totalCostSnapshot,
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
            stockReservationState,
            commercialDiscounts?.ToList() ?? [],
            priceOverrides?.ToList() ?? []);

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
        // Provider-backed Card/GCash enter AwaitingPayment and create payment attempts with
        // amount = sale.Total. Payment attempts require amount > 0. A fully discounted (₱0)
        // electronic sale would reserve stock and then be permanently stuck — reject at checkout.
        // Cash / ManualGCash may complete at ₱0. Utang still requires Total > 0 below.
        if (SalePaymentMethods.IsElectronic(paymentMethod) && total <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.SaleElectronicTotalMustBePositive,
                "Card and GCash checkouts require a total greater than zero. " +
                "A fully discounted sale must use Cash or Manual GCash.");
        }

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