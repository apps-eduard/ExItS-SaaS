using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

public sealed class SaleQueryService
{
    private readonly ISaleRepository _sales;
    private readonly IPOSCustomerRepository _customers;
    private readonly ICreditEntryRepository _credits;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly ICashierShiftRepository _shifts;
    private readonly IRegisterRepository _registers;
    private readonly IPosOperationalSetupRepository _operationalSetups;

    public SaleQueryService(
        ISaleRepository sales,
        IPOSCustomerRepository customers,
        ICreditEntryRepository credits,
        IOutstandingBalanceService outstanding,
        ICashierShiftRepository shifts,
        IRegisterRepository registers,
        IPosOperationalSetupRepository operationalSetups)
    {
        _sales = sales;
        _customers = customers;
        _credits = credits;
        _outstanding = outstanding;
        _shifts = shifts;
        _registers = registers;
        _operationalSetups = operationalSetups;
    }

    public async Task<PosSaleDto?> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var sale = await _sales
            .GetByIdAsync(orgId, SaleId.From(saleId), cancellationToken)
            .ConfigureAwait(false);
        if (sale is null)
        {
            return null;
        }

        return await MapEnrichedAsync(sale, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<PosSaleDto>> ListAsync(
        Guid organizationId,
        SaleFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _sales
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosSaleDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PosSaleDto Map(Sale sale)
    {
        var profitability = SaleProfitability.Compute(sale);
        return new(
            sale.Id.Value,
            sale.OrganizationId.Value,
            sale.SaleNumber,
            sale.Status.ToString(),
            SalePaymentMethods.ToCode(sale.PaymentMethod),
            sale.Subtotal,
            sale.Total,
            sale.TaxAmount,
            sale.AmountTendered,
            sale.ChangeAmount,
            sale.GCashReference,
            sale.RecordedAtUtc,
            sale.RecordedBy,
            sale.VoidedAtUtc,
            sale.VoidedBy,
            sale.VoidReason,
            sale.UpdatedAtUtc,
            sale.Lines
                .OrderBy(l => l.LineNumber)
                .Select(l => new PosSaleLineDto(
                    l.Id.Value,
                    l.ProductId.Value,
                    l.LineNumber,
                    l.NameSnapshot,
                    l.SkuSnapshot,
                    l.BarcodeSnapshot,
                    UnitOfMeasures.ToCode(l.UnitOfMeasureSnapshot),
                    SellingModes.ToCode(l.SellingModeSnapshot),
                    l.UnitPrice,
                    l.Quantity,
                    l.LineTotal,
                    l.GrossLineTotal,
                    l.LineDiscountAmount,
                    l.SaleDiscountAllocatedAmount,
                    l.UnitCostSnapshot,
                    l.LineCostSnapshot))
                .ToList(),
            sale.CustomerId?.Value,
            sale.LinkedCreditEntryId?.Value,
            ShiftId: sale.CashierShiftId?.Value,
            BuyerPartyKind: SaleBuyerParty.ToCode(sale.BuyerParty.Kind),
            BuyerDisplayNameSnapshot: sale.BuyerParty.DisplayNameSnapshot,
            BuyerPersonalPublicUserId: sale.BuyerParty.PersonalPublicUserId,
            BuyerOrganizationId: sale.BuyerParty.BuyerOrganizationId,
            BuyerPublicOrganizationId: sale.BuyerParty.BuyerPublicOrganizationId,
            DocumentKind: SalesDocumentWording.TransactionSummary,
            BranchId: sale.BranchId?.Value,
            GrossSubtotal: sale.GrossSubtotal,
            LineDiscountTotal: sale.LineDiscountTotal,
            SaleDiscountTotal: sale.SaleDiscountTotal,
            DiscountTotal: sale.DiscountTotal,
            PriceOverrides: sale.PriceOverrides.Count == 0
                ? null
                : sale.PriceOverrides
                    .Select(o =>
                    {
                        var line = sale.Lines.FirstOrDefault(l => l.Id == o.SaleLineId);
                        return new PosSaleQuotePriceOverrideDto(
                            line?.LineNumber ?? 0,
                            o.BaselineUnitPrice,
                            o.AppliedUnitPrice,
                            o.Reason);
                    })
                    .Where(o => o.LineNumber > 0)
                    .OrderBy(o => o.LineNumber)
                    .ToList(),
            CostStatus: ProductionCostStatuses.ToCode(sale.CostStatus),
            TotalCostSnapshot: sale.TotalCostSnapshot,
            GrossProfit: profitability?.GrossProfit,
            GrossMarginPercent: profitability?.GrossMarginPercent);
    }

    private async Task<PosSaleDto> MapEnrichedAsync(Sale sale, CancellationToken cancellationToken)
    {
        var dto = Map(sale);
        string? displayName = null;
        decimal? outstandingAfter = null;
        DateOnly? linkedDueDate = null;

        if (sale.CustomerId is not null)
        {
            var customer = await _customers
                .GetByIdAsync(sale.OrganizationId, sale.CustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (customer is not null)
            {
                displayName = customer.DisplayName;
                outstandingAfter = await _outstanding
                    .GetOutstandingAsync(sale.OrganizationId, sale.CustomerId, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (sale.LinkedCreditEntryId is not null)
            {
                var credit = await _credits
                    .GetByIdAsync(sale.OrganizationId, sale.CustomerId, sale.LinkedCreditEntryId, cancellationToken)
                    .ConfigureAwait(false);
                linkedDueDate = credit?.CurrentDueDate;
            }
        }

        // Prefer immutable buyer snapshot for receipts/history; fall back to live customer name.
        displayName = sale.BuyerParty.DisplayNameSnapshot ?? displayName;

        string? shiftNumber = null;
        if (sale.CashierShiftId is not null)
        {
            var shift = await _shifts
                .GetByIdAsync(sale.OrganizationId, sale.CashierShiftId.Value, cancellationToken)
                .ConfigureAwait(false);
            shiftNumber = shift?.ShiftNumber;
        }

        string? registerCode = null;
        string? registerName = null;
        if (sale.RegisterId is not null)
        {
            var register = await _registers
                .GetByIdAsync(sale.OrganizationId, sale.RegisterId, cancellationToken)
                .ConfigureAwait(false);
            registerCode = register?.RegisterCode;
            registerName = register?.Name;
        }

        string? storeDisplayName = null;
        string? currencyCode = null;
        string? taxPricingMode = null;
        string? receiptHeader = null;
        string? receiptFooter = null;
        string? businessAddress = null;
        string? contactPhone = null;
        var setup = await _operationalSetups
            .GetByOrganizationIdAsync(sale.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (setup is { IsCompleted: true })
        {
            storeDisplayName = setup.StoreDisplayName;
            currencyCode = setup.CurrencyCode;
            taxPricingMode = setup.TaxPricingMode.ToString();
            receiptHeader = setup.ReceiptHeader;
            receiptFooter = setup.ReceiptFooter;
            businessAddress = setup.BusinessAddress;
            contactPhone = setup.ContactPhone;
        }

        if (sale.CustomerId is null
            && shiftNumber is null
            && registerCode is null
            && storeDisplayName is null)
        {
            return dto;
        }

        return dto with
        {
            CustomerDisplayName = displayName,
            LinkedCreditDueDate = linkedDueDate,
            CustomerOutstandingAfter = outstandingAfter,
            ShiftNumber = shiftNumber,
            RegisterId = sale.RegisterId?.Value,
            RegisterCode = registerCode,
            RegisterName = registerName,
            StoreDisplayName = storeDisplayName,
            CurrencyCode = currencyCode,
            TaxPricingMode = taxPricingMode,
            ReceiptHeader = receiptHeader,
            ReceiptFooter = receiptFooter,
            BusinessAddress = businessAddress,
            ContactPhone = contactPhone
        };
    }
}

/// <summary>
/// Records a simple retail sale. The server is authoritative for every monetary value: it reloads
/// each requested product inside the caller's organization, requires it to be Active, and snapshots
/// the current selling price, name, SKU, barcode and unit of measure onto the line. Client-supplied
/// prices or names are never read.
///
/// Product-Based Utang creates the sale and a linked remarks credit in one transaction. Cash and
/// ManualGCash create the sale only. Tracked inventory is deducted atomically inside the same
/// checkout transaction (before credit creation for Utang).
/// </summary>
public sealed class CheckoutSale
{
    private readonly ISaleRepository _sales;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IPOSCustomerRepository _customers;
    private readonly ICreditEntryRepository _credits;
    private readonly ICreditDueDateChangeRepository _dueDateChanges;
    private readonly ISaleStockService _saleStock;
    private readonly ICashierShiftRepository _shifts;
    private readonly IPosOperationalSetupRepository _operationalSetups;
    private readonly IOrganizationTaxConfigurationCapabilityReader _taxConfiguration;
    private readonly IOfflinePriceAuthorityService _priceAuthorities;
    private readonly InventoryCostResolver _costResolver;
    private readonly IClock _clock;
    private readonly ICatalogProductAvailabilityResolver? _availability;
    private readonly IEffectivePriceResolver? _effectivePrices;

    public CheckoutSale(
        ISaleRepository sales,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IPOSCustomerRepository customers,
        ICreditEntryRepository credits,
        ICreditDueDateChangeRepository dueDateChanges,
        ISaleStockService saleStock,
        ICashierShiftRepository shifts,
        IPosOperationalSetupRepository operationalSetups,
        IOrganizationTaxConfigurationCapabilityReader taxConfiguration,
        IOfflinePriceAuthorityService priceAuthorities,
        InventoryCostResolver costResolver,
        IClock clock,
        ICatalogProductAvailabilityResolver? availability = null,
        IEffectivePriceResolver? effectivePrices = null)
    {
        _priceAuthorities = priceAuthorities;
        _costResolver = costResolver;
        _sales = sales;
        _products = products;
        _units = units;
        _customers = customers;
        _credits = credits;
        _dueDateChanges = dueDateChanges;
        _saleStock = saleStock;
        _shifts = shifts;
        _operationalSetups = operationalSetups;
        _taxConfiguration = taxConfiguration;
        _clock = clock;
        _availability = availability;
        _effectivePrices = effectivePrices;
    }

    public async Task<ApplicationResult<Sale>> ExecuteAsync(
        Guid organizationId,
        IReadOnlyList<CheckoutSaleLineRequest>? lines,
        string paymentMethod,
        Guid actorId,
        decimal? amountTendered = null,
        string? gcashReference = null,
        Guid? clientSaleId = null,
        Guid? customerId = null,
        DateOnly? dueDate = null,
        Guid? creditEntryId = null,
        Guid? shiftId = null,
        string? buyerPartyKind = null,
        string? buyerDisplayNameSnapshot = null,
        string? buyerPersonalPublicUserId = null,
        Guid? buyerOrganizationId = null,
        string? buyerPublicOrganizationId = null,
        Guid? branchId = null,
        IReadOnlyList<CommercialDiscountIntentRequest>? discounts = null,
        IReadOnlyList<SalePriceOverrideIntentRequest>? priceOverrides = null,
        bool allowUnlimitedSalePriceOverride = false,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<Sale>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to record a sale.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            var openShift = await _shifts
                .FindOpenForActorAsync(orgId, actorId, cancellationToken)
                .ConfigureAwait(false);
            if (openShift is null)
            {
                return ApplicationResult<Sale>.Failure(
                    ApplicationErrorCodes.CashierShiftNoOpenShift,
                    "Checkout requires an open cashier shift for this actor.");
            }

            if (shiftId is not null && shiftId.Value != openShift.Id.Value)
            {
                return ApplicationResult<Sale>.Failure(
                    ApplicationErrorCodes.CashierShiftMismatch,
                    "The supplied shift id does not match the actor's open shift.");
            }

            var linkedShiftId = openShift.Id;
            if (openShift.RegisterId is null)
            {
                return ApplicationResult<Sale>.Failure(
                    DomainErrorCodes.SaleRegisterRequired,
                    "Checkout requires a register inherited from the open shift. Re-open the shift on an Active register.");
            }

            var linkedRegisterId = openShift.RegisterId;

            if (clientSaleId is not null)
            {
                var existing = await _sales
                    .GetByIdAsync(orgId, SaleId.From(clientSaleId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<Sale>.Success(existing);
                }
            }

            var method = SalePaymentMethods.Parse(paymentMethod);
            var isElectronic = SalePaymentMethods.IsElectronic(method);
            var isUtang = method == SalePaymentMethod.Utang;

            if (!isUtang && (dueDate is not null || creditEntryId is not null))
            {
                return ApplicationResult<Sale>.Failure(
                    DomainErrorCodes.SaleCashMustNotLinkCredit,
                    "Cash, Card, GCash, and Manual GCash sales must not include due date or credit entry fields.");
            }

            POSCustomerId? linkedCustomerId = null;
            CreditEntryId? linkedCreditEntryId = null;
            POSCustomer? loadedCustomer = null;

            if (customerId is not null && customerId != Guid.Empty)
            {
                linkedCustomerId = POSCustomerId.From(customerId.Value);
                loadedCustomer = await _customers
                    .GetByIdAsync(orgId, linkedCustomerId, cancellationToken)
                    .ConfigureAwait(false);
                if (loadedCustomer is null)
                {
                    return ApplicationResult<Sale>.Failure(
                        ApplicationErrorCodes.CustomerNotFound,
                        "Customer was not found.");
                }

                if (loadedCustomer.Status != CustomerStatus.Active)
                {
                    return ApplicationResult<Sale>.Failure(
                        DomainErrorCodes.CustomerNotActive,
                        "Sales can only attach an active customer.");
                }
            }

            var buyerPartyResult = SaleBuyerPartyFactory.TryCreate(
                buyerPartyKind,
                buyerDisplayNameSnapshot,
                buyerPersonalPublicUserId,
                buyerOrganizationId,
                buyerPublicOrganizationId,
                loadedCustomer);
            if (!buyerPartyResult.IsSuccess)
            {
                return ApplicationResult<Sale>.Failure(
                    buyerPartyResult.ErrorCode!,
                    buyerPartyResult.ErrorMessage!);
            }

            var resolvedBuyerParty = buyerPartyResult.Value!;
            try
            {
                resolvedBuyerParty.EnsureConsistentWith(linkedCustomerId);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<Sale>.Failure(ex.ErrorCode, ex.Message);
            }

            if (isUtang)
            {
                if (linkedCustomerId is null)
                {
                    return ApplicationResult<Sale>.Failure(
                        DomainErrorCodes.SaleUtangCustomerRequired,
                        "Product-Based Utang requires a customer.");
                }

                linkedCreditEntryId = creditEntryId is null || creditEntryId == Guid.Empty
                    ? CreditEntryId.New()
                    : CreditEntryId.From(creditEntryId.Value);

                var existingCredit = await _credits
                    .GetByIdAsync(orgId, linkedCustomerId, linkedCreditEntryId, cancellationToken)
                    .ConfigureAwait(false);
                if (existingCredit is not null)
                {
                    // Idempotent client credit id already used — reject rather than orphan a sale.
                    if (existingCredit.SourceSaleId is not null && clientSaleId is not null
                        && existingCredit.SourceSaleId.Value == clientSaleId.Value)
                    {
                        var linkedSale = await _sales
                            .GetByIdAsync(orgId, SaleId.From(clientSaleId.Value), cancellationToken)
                            .ConfigureAwait(false);
                        if (linkedSale is not null)
                        {
                            return ApplicationResult<Sale>.Success(linkedSale);
                        }
                    }

                    return ApplicationResult<Sale>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "The supplied credit entry id is already in use.");
                }
            }

            var resolved = await ResolveDraftsAsync(
                    orgId,
                    lines,
                    clientSaleId,
                    discounts,
                    priceOverrides,
                    branchId,
                    allowOfflinePriceAuthorities: true,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!resolved.IsSuccess)
            {
                return ApplicationResult<Sale>.Failure(resolved.ErrorCode!, resolved.ErrorMessage!);
            }

            var drafts = resolved.Value!.Drafts;
            var byId = resolved.Value!.ProductsById;

            drafts = (await _costResolver
                .EnrichDraftsWithCostsAsync(orgId, drafts, cancellationToken)
                .ConfigureAwait(false)).ToList();

            var intentsResult = TryParseIntents(discounts);
            if (!intentsResult.IsSuccess)
            {
                return ApplicationResult<Sale>.Failure(intentsResult.ErrorCode!, intentsResult.ErrorMessage!);
            }

            var intents = intentsResult.Value!;

            var overrideIntentsResult = TryParsePriceOverrideIntents(priceOverrides);
            if (!overrideIntentsResult.IsSuccess)
            {
                return ApplicationResult<Sale>.Failure(
                    overrideIntentsResult.ErrorCode!,
                    overrideIntentsResult.ErrorMessage!);
            }

            var overrideIntents = overrideIntentsResult.Value!;
            var allowUnlimited = allowUnlimitedSalePriceOverride;

            var utcNow = _clock.UtcNow;
            var capturedCustomerId = linkedCustomerId;
            var capturedCreditEntryId = linkedCreditEntryId;
            var capturedDueDate = dueDate;
            var capturedActorId = actorId;
            var productsById = byId;

            // Tax must be computed from the NET (post-discount) subtotal, so override + discount math
            // runs first. Sale.Checkout independently recomputes the same numbers from the same intents.
            var moneyPreview = Sale.QuoteCheckoutMoney(
                orgId,
                drafts,
                intents,
                overrideIntents,
                allowUnlimited);

            decimal taxAmount = 0;
            TaxPricingMode? taxPricingMode = null;
            var setup = await _operationalSetups
                .GetByOrganizationIdAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
            var taxConfigurationEnabled = await _taxConfiguration
                .IsTaxConfigurationEnabledAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (OperationalSetupTaxCalculator.ShouldApplyConfiguredTax(taxConfigurationEnabled, setup))
            {
                taxPricingMode = setup!.TaxPricingMode;
                taxAmount = OperationalSetupTaxCalculator.ComputeTaxAmount(
                    moneyPreview.Discounts.NetSubtotal,
                    setup.TaxRatePercent,
                    setup.TaxPricingMode);
            }

            var capturedTaxAmount = taxAmount;
            var capturedTaxPricingMode = taxPricingMode;

            var sale = await _sales
                .CheckoutAsync(
                    orgId,
                    SaleNumbers.BusinessDateOf(utcNow),
                    saleNumber => Sale.Checkout(
                        orgId,
                        saleNumber,
                        method,
                        drafts,
                        actorId,
                        utcNow,
                        amountTendered,
                        gcashReference,
                        clientSaleId is null ? null : SaleId.From(clientSaleId.Value),
                        capturedCustomerId,
                        capturedCreditEntryId,
                        linkedShiftId,
                        linkedRegisterId,
                        capturedTaxAmount,
                        capturedTaxPricingMode,
                        resolvedBuyerParty,
                        branchId is Guid saleBranch ? PosBranchId.From(saleBranch) : null,
                        intents,
                        overrideIntents,
                        allowUnlimited),
                    async (createdSale, ct) =>
                    {
                        // Electronic Card/GCash sales await payment — reserve stock until Paid/Released.
                        if (isElectronic)
                        {
                            await _saleStock
                                .EnsureAvailableForSaleAsync(orgId, createdSale, ct, branchId)
                                .ConfigureAwait(false);
                            await _saleStock
                                .ReserveForAwaitingPaymentAsync(createdSale, capturedActorId, utcNow, ct, branchId)
                                .ConfigureAwait(false);
                            return;
                        }

                        await _saleStock
                            .EnsureAvailableForSaleAsync(orgId, createdSale, ct, branchId)
                            .ConfigureAwait(false);
                        await _saleStock
                            .DeductForSaleAsync(orgId, createdSale, productsById, capturedActorId, utcNow, ct, branchId)
                            .ConfigureAwait(false);

                        if (!isUtang)
                        {
                            return;
                        }

                        var entry = CreditEntry.Create(
                            orgId,
                            capturedCustomerId!,
                            createdSale.Total,
                            ProductBasedUtangRemarks.ForSaleNumber(createdSale.SaleNumber),
                            utcNow,
                            capturedCreditEntryId,
                            createdSale.Id);

                        if (capturedDueDate is not null)
                        {
                            var change = CreditDueDateChange.Create(
                                orgId,
                                entry.Id,
                                entry.CustomerId,
                                previousDueDate: null,
                                newDueDate: capturedDueDate,
                                ProductBasedUtangRemarks.InitialDueDateReason,
                                capturedActorId,
                                utcNow);
                            entry.ApplyCurrentDueDate(capturedDueDate);
                            await _dueDateChanges.AddAsync(change, ct).ConfigureAwait(false);
                        }

                        await _credits.AddAsync(entry, ct).ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<Sale>.Success(sale);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Sale>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Sale>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    /// <summary>
    /// Prices a cart and previews price overrides, commercial discounts, and tax without recording
    /// anything: no sale, no sale number, no stock movement, no credit entry. Checkout revalidates
    /// independently.
    /// </summary>
    public async Task<ApplicationResult<PosSaleQuoteDto>> QuoteAsync(
        Guid organizationId,
        IReadOnlyList<CheckoutSaleLineRequest>? lines,
        IReadOnlyList<CommercialDiscountIntentRequest>? discounts = null,
        IReadOnlyList<SalePriceOverrideIntentRequest>? priceOverrides = null,
        bool allowUnlimitedSalePriceOverride = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            var resolved = await ResolveDraftsAsync(
                    orgId,
                    lines,
                    clientSaleId: null,
                    discounts,
                    priceOverrides,
                    branchId: null,
                    allowOfflinePriceAuthorities: false,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!resolved.IsSuccess)
            {
                return ApplicationResult<PosSaleQuoteDto>.Failure(resolved.ErrorCode!, resolved.ErrorMessage!);
            }

            var intentsResult = TryParseIntents(discounts);
            if (!intentsResult.IsSuccess)
            {
                return ApplicationResult<PosSaleQuoteDto>.Failure(
                    intentsResult.ErrorCode!,
                    intentsResult.ErrorMessage!);
            }

            var overrideIntentsResult = TryParsePriceOverrideIntents(priceOverrides);
            if (!overrideIntentsResult.IsSuccess)
            {
                return ApplicationResult<PosSaleQuoteDto>.Failure(
                    overrideIntentsResult.ErrorCode!,
                    overrideIntentsResult.ErrorMessage!);
            }

            var baselineDrafts = resolved.Value!.Drafts;
            var money = Sale.QuoteCheckoutMoney(
                orgId,
                baselineDrafts,
                intentsResult.Value,
                overrideIntentsResult.Value,
                allowUnlimitedSalePriceOverride);

            decimal taxAmount = 0;
            TaxPricingMode? taxPricingMode = null;
            var setup = await _operationalSetups
                .GetByOrganizationIdAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
            var taxConfigurationEnabled = await _taxConfiguration
                .IsTaxConfigurationEnabledAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (OperationalSetupTaxCalculator.ShouldApplyConfiguredTax(taxConfigurationEnabled, setup))
            {
                taxPricingMode = setup!.TaxPricingMode;
                taxAmount = OperationalSetupTaxCalculator.ComputeTaxAmount(
                    money.Discounts.NetSubtotal,
                    setup.TaxRatePercent,
                    setup.TaxPricingMode);
            }

            var total = taxPricingMode == TaxPricingMode.TaxExclusive
                ? SaleMoney.RoundMoney(money.Discounts.NetSubtotal + taxAmount)
                : money.Discounts.NetSubtotal;

            var overridesByLine = money.PriceOverrides.Adjustments.ToDictionary(a => a.LineNumber);

            var quoteLines = money.Discounts.Lines
                .OrderBy(l => l.LineNumber)
                .Select(l =>
                {
                    var draft = money.PricedDrafts[l.LineNumber - 1];
                    var baseline = baselineDrafts[l.LineNumber - 1].UnitPrice;
                    return new PosSaleQuoteLineDto(
                        l.LineNumber,
                        draft.ProductId.Value,
                        draft.NameSnapshot,
                        UnitOfMeasures.ToCode(draft.UnitOfMeasureSnapshot),
                        SellingModes.ToCode(draft.SellingModeSnapshot),
                        draft.UnitPrice,
                        draft.EnteredQuantity ?? draft.Quantity,
                        l.GrossLineTotal,
                        l.LineDiscountAmount,
                        l.SaleDiscountAllocatedAmount,
                        l.NetLineTotal,
                        BaselineUnitPrice: overridesByLine.ContainsKey(l.LineNumber) ? baseline : null);
                })
                .ToList();

            return ApplicationResult<PosSaleQuoteDto>.Success(new PosSaleQuoteDto(
                money.Discounts.GrossSubtotal,
                money.Discounts.LineDiscountTotal,
                money.Discounts.SaleDiscountTotal,
                money.Discounts.DiscountTotal,
                money.Discounts.NetSubtotal,
                taxAmount,
                total,
                taxPricingMode?.ToString(),
                quoteLines,
                money.Discounts.Adjustments
                    .Select(a => new PosSaleQuoteDiscountDto(
                        SaleCommercialDiscountRules.ToCode(a.Scope),
                        SaleCommercialDiscountRules.ToCode(a.Method),
                        a.RequestedValue,
                        a.CalculatedAmount,
                        a.Reason,
                        a.LineNumber))
                    .ToList(),
                money.PriceOverrides.Adjustments
                    .Select(a => new PosSaleQuotePriceOverrideDto(
                        a.LineNumber,
                        a.BaselineUnitPrice,
                        a.AppliedUnitPrice,
                        a.Reason))
                    .ToList()));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosSaleQuoteDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    /// <summary>
    /// Translates client discount intents into domain intents. Only scope/method/value/reason are
    /// read; any amount the client believes applies is ignored.
    /// </summary>
    private static ApplicationResult<IReadOnlyList<CommercialDiscountIntent>?> TryParseIntents(
        IReadOnlyList<CommercialDiscountIntentRequest>? discounts)
    {
        if (discounts is null || discounts.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<CommercialDiscountIntent>?>.Success(null);
        }

        var intents = new List<CommercialDiscountIntent>(discounts.Count);
        foreach (var requested in discounts)
        {
            if (requested is null)
            {
                return ApplicationResult<IReadOnlyList<CommercialDiscountIntent>?>.Failure(
                    DomainErrorCodes.SaleDiscountInvalidScope,
                    "A commercial discount entry was empty.");
            }

            try
            {
                intents.Add(new CommercialDiscountIntent(
                    SaleCommercialDiscountRules.ParseScope(requested.Scope),
                    SaleCommercialDiscountRules.ParseMethod(requested.Method),
                    requested.Value,
                    requested.Reason,
                    requested.ProductId is Guid productId && productId != Guid.Empty
                        ? CatalogProductId.From(productId)
                        : null,
                    requested.LineNumber));
            }
            catch (DomainException ex)
            {
                return ApplicationResult<IReadOnlyList<CommercialDiscountIntent>?>.Failure(
                    ex.ErrorCode,
                    ex.Message);
            }
        }

        return ApplicationResult<IReadOnlyList<CommercialDiscountIntent>?>.Success(intents);
    }

    private static ApplicationResult<IReadOnlyList<SalePriceOverrideIntent>?> TryParsePriceOverrideIntents(
        IReadOnlyList<SalePriceOverrideIntentRequest>? priceOverrides)
    {
        if (priceOverrides is null || priceOverrides.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<SalePriceOverrideIntent>?>.Success(null);
        }

        var intents = new List<SalePriceOverrideIntent>(priceOverrides.Count);
        foreach (var requested in priceOverrides)
        {
            if (requested is null)
            {
                return ApplicationResult<IReadOnlyList<SalePriceOverrideIntent>?>.Failure(
                    DomainErrorCodes.SalePriceOverrideLineUnmatched,
                    "A sale price override entry was empty.");
            }

            intents.Add(new SalePriceOverrideIntent(
                requested.RequestedUnitPrice,
                requested.Reason,
                requested.ProductId is Guid productId && productId != Guid.Empty
                    ? CatalogProductId.From(productId)
                    : null,
                requested.LineNumber,
                requested.ExpectedBaselineUnitPrice));
        }

        return ApplicationResult<IReadOnlyList<SalePriceOverrideIntent>?>.Success(intents);
    }

    private async Task<ApplicationResult<ResolvedCheckoutDrafts>> ResolveDraftsAsync(
        PosOrganizationId orgId,
        IReadOnlyList<CheckoutSaleLineRequest>? lines,
        Guid? clientSaleId,
        IReadOnlyList<CommercialDiscountIntentRequest>? discounts,
        IReadOnlyList<SalePriceOverrideIntentRequest>? priceOverrides,
        Guid? branchId,
        bool allowOfflinePriceAuthorities,
        CancellationToken cancellationToken)
    {
        if (lines is null || lines.Count == 0)
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                DomainErrorCodes.SaleRequiresAtLeastOneLine,
                "A sale must contain at least one line.");
        }

        var productIds = lines
            .Where(l => l is not null)
            .Select(l => CatalogProductId.From(l.ProductId))
            .Distinct()
            .ToList();
        var products = await _products
            .ListByIdsAsync(orgId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byId = products.ToDictionary(p => p.Id.Value);

        // Commercial offering is branch-scoped. When branchId is omitted (tests / legacy), skip the check.
        IReadOnlyDictionary<Guid, CatalogProductOfferingResult>? offerings = null;
        if (branchId is Guid bid && bid != Guid.Empty && _availability is not null)
        {
            offerings = await _availability
                .ResolveForBranchAsync(orgId, PosBranchId.From(bid), products, cancellationToken)
                .ConfigureAwait(false);
        }

        var drafts = new List<SaleLineDraft>();
        var usesPriceAuthorities = CheckoutSaleLineAuthorities.RequestUsesOfflinePriceAuthorities(lines);
        if (usesPriceAuthorities && !allowOfflinePriceAuthorities)
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                ApplicationErrorCodes.OfflinePriceAuthorityOnlineNotSupported,
                "Offline price authorities are accepted at checkout only; an online quote prices from the live catalog.");
        }

        if (usesPriceAuthorities && (clientSaleId is null || clientSaleId == Guid.Empty))
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                ApplicationErrorCodes.OfflinePriceAuthorityRequestInvalid,
                "An offline authority sale must carry the client SaleId it was queued under.");
        }

        // The lease path fails closed the same way the snapshot path does: neither carries the
        // server-side discount or override math that would be needed to honour these intents.
        if (usesPriceAuthorities && discounts is { Count: > 0 })
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                ApplicationErrorCodes.SaleDiscountOfflineNotSupported,
                "Commercial discounts cannot be applied to an offline sale. Record the sale online.");
        }

        if (usesPriceAuthorities && priceOverrides is { Count: > 0 })
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                ApplicationErrorCodes.SalePriceOverrideOfflineNotSupported,
                "Sale price overrides cannot be applied to an offline sale. Record the sale online.");
        }

        var usesTrustedSnapshots = !usesPriceAuthorities
            && CheckoutSaleLineSnapshots.RequestUsesTrustedSnapshots(lines);
        if (usesTrustedSnapshots
            && (clientSaleId is null || clientSaleId == Guid.Empty))
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                ApplicationErrorCodes.SaleSnapshotInvalid,
                "Trusted sale line snapshots require a client SaleId (offline sync). Online carts must omit snapshot fields.");
        }

        // Fail closed: an offline snapshot payload arrives with client-computed line totals that were
        // produced without any discount math, so its arithmetic and a discount request cannot both be
        // honoured. Legacy offline sync without discounts keeps working unchanged.
        if (usesTrustedSnapshots && discounts is { Count: > 0 })
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                ApplicationErrorCodes.SaleDiscountOfflineNotSupported,
                "Commercial discounts cannot be applied to an offline sale snapshot. Record the sale online.");
        }

        // Fail closed: offline snapshots also cannot carry unit-price overrides (same arithmetic
        // fidelity constraint as commercial discounts).
        if (usesTrustedSnapshots && priceOverrides is { Count: > 0 })
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                ApplicationErrorCodes.SalePriceOverrideOfflineNotSupported,
                "Sale price overrides cannot be applied to an offline sale snapshot. Record the sale online.");
        }

        var unitIds = lines
            .Where(l => l?.SellingUnitId is not null)
            .Select(l => ProductUnitId.From(l!.SellingUnitId!.Value))
            .Distinct()
            .ToList();
        var unitsById = new Dictionary<Guid, CatalogProductUnit>();
        foreach (var unitId in unitIds)
        {
            var unit = await _units.GetByIdAsync(orgId, unitId, cancellationToken).ConfigureAwait(false);
            if (unit is not null)
            {
                unitsById[unit.Id.Value] = unit;
            }
        }

        IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult>? effectivePrices = null;
        if (branchId is Guid effectiveBranchId
            && effectiveBranchId != Guid.Empty
            && _effectivePrices is not null)
        {
            var unitsByProduct = unitsById.Values
                .GroupBy(u => u.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<CatalogProductUnit>)g.ToList());
            effectivePrices = await _effectivePrices
                .ResolveAsync(
                    orgId,
                    PosBranchId.From(effectiveBranchId),
                    products,
                    unitsByProduct,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        decimal ResolveBasePrice(CatalogProduct product) =>
            effectivePrices?.TryGetValue(EffectivePriceKeys.ForBaseProduct(product.Id.Value), out var baseResult) == true
                ? baseResult.EffectivePrice
                : product.SellingPrice;

        decimal? ResolveUnitPrice(CatalogProduct product, CatalogProductUnit unit) =>
            effectivePrices?.TryGetValue(
                EffectivePriceKeys.ForSellUnit(product.Id.Value, unit.Id.Value),
                out var unitResult) == true
                ? unitResult.EffectivePrice
                : null;

        if (usesPriceAuthorities)
        {
            foreach (var line in lines)
            {
                if (line is null)
                {
                    continue;
                }

                if (!byId.TryGetValue(line.ProductId, out var product))
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.SaleProductNotFound,
                        "One or more products in the cart were not found in this organization.");
                }

                if (product.Status != CatalogProductStatus.Active)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.SaleProductNotActive,
                        $"'{product.Name}' is inactive and cannot be sold. Remove it from the cart or reactivate it.");
                }

                if (!product.CanBeSold)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.SaleProductNotSellable,
                        $"'{product.Name}' is not sold as-is and cannot be added to a sale.");
                }

                if (offerings is not null
                    && offerings.TryGetValue(product.Id.Value, out var offer)
                    && !offer.IsOffered)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.ProductNotOfferedAtBranch,
                        $"'{product.Name}' is not offered at this branch.");
                }

                CatalogProductUnit? authoritySellingUnit = null;
                if (line.SellingUnitId is not null)
                {
                    unitsById.TryGetValue(line.SellingUnitId.Value, out authoritySellingUnit);
                }

                var authorityDraft = CheckoutSaleLineAuthorities.TryCreateDraftFromAuthority(
                    line,
                    product,
                    authoritySellingUnit,
                    _priceAuthorities,
                    orgId.Value,
                    branchId);
                if (!authorityDraft.IsSuccess)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        authorityDraft.ErrorCode!,
                        authorityDraft.ErrorMessage!);
                }

                drafts.Add(authorityDraft.Value!);
            }
        }
        else if (usesTrustedSnapshots)
        {
            foreach (var line in lines)
            {
                if (line is null)
                {
                    continue;
                }

                if (!byId.TryGetValue(line.ProductId, out var product))
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.SaleProductNotFound,
                        "One or more products in the cart were not found in this organization.");
                }

                if (product.Status != CatalogProductStatus.Active)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.SaleProductNotActive,
                        $"'{product.Name}' is inactive and cannot be sold. Remove it from the cart or reactivate it.");
                }

                if (!product.CanBeSold)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.SaleProductNotSellable,
                        $"'{product.Name}' is not sold as-is and cannot be added to a sale.");
                }

                if (offerings is not null
                    && offerings.TryGetValue(product.Id.Value, out var offer)
                    && !offer.IsOffered)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        ApplicationErrorCodes.ProductNotOfferedAtBranch,
                        $"'{product.Name}' is not offered at this branch.");
                }

                CatalogProductUnit? sellingUnit = null;
                if (line.SellingUnitId is not null)
                {
                    unitsById.TryGetValue(line.SellingUnitId.Value, out sellingUnit);
                }

                var snapshotDraft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, product, sellingUnit);
                if (!snapshotDraft.IsSuccess)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        snapshotDraft.ErrorCode!,
                        snapshotDraft.ErrorMessage!);
                }

                drafts.Add(snapshotDraft.Value!);
            }
        }
        else
        {
            var usesUnits = lines.Any(l => l?.SellingUnitId is not null || l?.EnteredQuantity is not null);
            if (usesUnits)
            {
                foreach (var line in lines)
                {
                    if (line is null)
                    {
                        continue;
                    }

                    if (!byId.TryGetValue(line.ProductId, out var product))
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.SaleProductNotFound,
                            "One or more products in the cart were not found in this organization.");
                    }

                    if (product.Status != CatalogProductStatus.Active)
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.SaleProductNotActive,
                            $"'{product.Name}' is inactive and cannot be sold. Remove it from the cart or reactivate it.");
                    }

                    if (!product.CanBeSold)
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.SaleProductNotSellable,
                            $"'{product.Name}' is not sold as-is and cannot be added to a sale.");
                    }

                    if (offerings is not null
                        && offerings.TryGetValue(product.Id.Value, out var offer)
                        && !offer.IsOffered)
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.ProductNotOfferedAtBranch,
                            $"'{product.Name}' is not offered at this branch.");
                    }

                    CatalogProductUnit? sellingUnit = null;
                    if (line.SellingUnitId is not null)
                    {
                        unitsById.TryGetValue(line.SellingUnitId.Value, out sellingUnit);
                    }

                    var onlineDraft = CheckoutSaleLineSnapshots.TryCreateOnlineDraft(
                        line,
                        product,
                        sellingUnit,
                        ResolveBasePrice(product),
                        sellingUnit is not null ? ResolveUnitPrice(product, sellingUnit) : null);
                    if (!onlineDraft.IsSuccess)
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            onlineDraft.ErrorCode!,
                            onlineDraft.ErrorMessage!);
                    }

                    drafts.Add(onlineDraft.Value!);
                }
            }
            else
            {
                var requested = CombineRequestedQuantities(lines);
                if (requested.Count == 0)
                {
                    return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                        DomainErrorCodes.SaleRequiresAtLeastOneLine,
                        "A sale must contain at least one line.");
                }

                drafts = new List<SaleLineDraft>(requested.Count);
                foreach (var (productId, quantity) in requested)
                {
                    if (!byId.TryGetValue(productId, out var product))
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.SaleProductNotFound,
                            "One or more products in the cart were not found in this organization.");
                    }

                    if (product.Status != CatalogProductStatus.Active)
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.SaleProductNotActive,
                            $"'{product.Name}' is inactive and cannot be sold. Remove it from the cart or reactivate it.");
                    }

                    if (!product.CanBeSold)
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.SaleProductNotSellable,
                            $"'{product.Name}' is not sold as-is and cannot be added to a sale.");
                    }

                    if (offerings is not null
                        && offerings.TryGetValue(product.Id.Value, out var offer)
                        && !offer.IsOffered)
                    {
                        return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                            ApplicationErrorCodes.ProductNotOfferedAtBranch,
                            $"'{product.Name}' is not offered at this branch.");
                    }

                    drafts.Add(new SaleLineDraft(
                        product.Id,
                        product.Name,
                        product.Sku,
                        product.Barcode,
                        product.UnitOfMeasure,
                        ResolveBasePrice(product),
                        quantity,
                        product.SellingMode));
                }
            }
        }

        if (drafts.Count == 0)
        {
            return ApplicationResult<ResolvedCheckoutDrafts>.Failure(
                DomainErrorCodes.SaleRequiresAtLeastOneLine,
                "A sale must contain at least one line.");
        }

        return ApplicationResult<ResolvedCheckoutDrafts>.Success(new ResolvedCheckoutDrafts(drafts, byId));
    }

    /// <summary>Server-priced checkout lines plus the products they were priced from.</summary>
    private sealed record ResolvedCheckoutDrafts(
        List<SaleLineDraft> Drafts,
        Dictionary<Guid, CatalogProduct> ProductsById);

    /// <summary>
    /// Folds repeated scans of the same product into a single line by summing quantities, matching
    /// the cart behaviour clients present. Ordering follows first appearance in the request.
    /// </summary>
    private static List<(Guid ProductId, decimal Quantity)> CombineRequestedQuantities(
        IReadOnlyList<CheckoutSaleLineRequest> lines)
    {
        var order = new List<Guid>();
        var totals = new Dictionary<Guid, decimal>();

        foreach (var line in lines)
        {
            if (line is null)
            {
                continue;
            }

            if (!totals.TryGetValue(line.ProductId, out var running))
            {
                order.Add(line.ProductId);
                running = 0m;
            }

            totals[line.ProductId] = running + line.Quantity;
        }

        return order.Select(id => (id, totals[id])).ToList();
    }
}

/// <summary>
/// Voids a recorded sale with a required reason and actor. Cash/ManualGCash voids the sale and
/// restores tracked stock. Product-Based Utang voids the sale, reverses the linked credit, and
/// restores stock in one serializable transaction.
/// </summary>
public sealed class VoidSale
{
    private readonly ISaleRepository _sales;
    private readonly ISaleMutationLock _saleMutationLock;
    private readonly ICreditEntryRepository _credits;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly ISaleStockService _saleStock;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public VoidSale(
        ISaleRepository sales,
        ISaleMutationLock saleMutationLock,
        ICreditEntryRepository credits,
        IOutstandingBalanceService outstanding,
        ISaleStockService saleStock,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _sales = sales;
        _saleMutationLock = saleMutationLock;
        _credits = credits;
        _outstanding = outstanding;
        _saleStock = saleStock;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Sale>> ExecuteAsync(
        Guid organizationId,
        Guid saleId,
        string reason,
        Guid actorId,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<Sale>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to void a sale.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = SaleId.From(saleId);

        try
        {
            PersistenceConflictException? lastConflict = null;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    return await _unitOfWork
                        .ExecuteInSerializableTransactionAsync(async ct =>
                        {
                            await _saleMutationLock.AcquireAsync(orgId, id, ct).ConfigureAwait(false);

                            var current = await _sales.GetByIdAsync(orgId, id, ct).ConfigureAwait(false);
                            if (current is null)
                            {
                                return ApplicationResult<Sale>.Failure(
                                    ApplicationErrorCodes.SaleNotFound,
                                    "Sale was not found.");
                            }

                            if (await _sales.HasReturnsForSaleAsync(orgId, id, ct).ConfigureAwait(false))
                            {
                                return ApplicationResult<Sale>.Failure(
                                    ApplicationErrorCodes.SaleVoidBlockedByReturns,
                                    "Voiding is blocked because this sale has one or more returns.");
                            }

                            if (current.PaymentMethod == SalePaymentMethod.Utang)
                            {
                                if (current.LinkedCreditEntryId is null || current.CustomerId is null)
                                {
                                    return ApplicationResult<Sale>.Failure(
                                        DomainErrorCodes.SaleUtangLinkageInvalid,
                                        "Utang sale is missing customer or linked credit entry.");
                                }

                                var credit = await _credits
                                    .GetByIdAsync(orgId, current.CustomerId, current.LinkedCreditEntryId, ct)
                                    .ConfigureAwait(false);
                                if (credit is null)
                                {
                                    return ApplicationResult<Sale>.Failure(
                                        ApplicationErrorCodes.CreditEntryNotFound,
                                        "Linked credit entry was not found.");
                                }

                                if (credit.SourceSaleId is null || credit.SourceSaleId.Value != current.Id.Value)
                                {
                                    return ApplicationResult<Sale>.Failure(
                                        DomainErrorCodes.SaleUtangLinkageInvalid,
                                        "Linked credit entry does not reference this sale.");
                                }

                                if (credit.Status == CreditEntryStatus.Reversed
                                    && current.Status == SaleStatus.Completed)
                                {
                                    return ApplicationResult<Sale>.Failure(
                                        ApplicationErrorCodes.SaleVoidBlockedBySubsequentUtangActivity,
                                        "The linked Utang credit is already reversed; voiding this sale is blocked.");
                                }

                                if (credit.Status == CreditEntryStatus.Active)
                                {
                                    var outstanding = await _outstanding
                                        .GetOutstandingAsync(orgId, current.CustomerId, ct)
                                        .ConfigureAwait(false);
                                    if (outstanding - credit.Amount < 0m)
                                    {
                                        return ApplicationResult<Sale>.Failure(
                                            ApplicationErrorCodes.SaleVoidBlockedBySubsequentUtangActivity,
                                            "Voiding this Utang sale would make outstanding negative because of subsequent repayments. Reverse those repayments first, or leave the sale as recorded.");
                                    }
                                }

                                current.Void(reason, actorId, _clock.UtcNow);
                                await _sales.UpdateAsync(current, ct).ConfigureAwait(false);

                                if (credit.Status == CreditEntryStatus.Active)
                                {
                                    credit.Reverse(reason, _clock.UtcNow);
                                    await _credits.UpdateAsync(credit, ct).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                current.Void(reason, actorId, _clock.UtcNow);
                                await _sales.UpdateAsync(current, ct).ConfigureAwait(false);
                            }

                            if (current.StockReservationState == SaleStockReservationState.Reserved)
                            {
                                await _saleStock
                                    .ReleaseIfReservedAsync(current, _clock.UtcNow, ct)
                                    .ConfigureAwait(false);
                                await _sales.UpdateAsync(current, ct).ConfigureAwait(false);
                            }

                            await _saleStock
                                .RestoreForSaleVoidAsync(orgId, current, actorId, reason, _clock.UtcNow, ct, branchId)
                                .ConfigureAwait(false);

                            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                            return ApplicationResult<Sale>.Success(current);
                        }, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (PersistenceConflictException ex)
                {
                    lastConflict = ex;
                }
            }

            return ApplicationResult<Sale>.Failure(
                lastConflict!.ErrorCode,
                lastConflict.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Sale>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Sale>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
