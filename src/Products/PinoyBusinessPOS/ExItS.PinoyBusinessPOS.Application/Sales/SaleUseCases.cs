using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
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

    public static PosSaleDto Map(Sale sale) =>
        new(
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
                    l.LineTotal))
                .ToList(),
            sale.CustomerId?.Value,
            sale.LinkedCreditEntryId?.Value,
            ShiftId: sale.CashierShiftId?.Value,
            BuyerPartyKind: SaleBuyerParty.ToCode(sale.BuyerParty.Kind),
            BuyerDisplayNameSnapshot: sale.BuyerParty.DisplayNameSnapshot,
            BuyerPersonalPublicUserId: sale.BuyerParty.PersonalPublicUserId,
            BuyerOrganizationId: sale.BuyerParty.BuyerOrganizationId,
            BuyerPublicOrganizationId: sale.BuyerParty.BuyerPublicOrganizationId,
            DocumentKind: SalesDocumentWording.TransactionSummary);

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
    private readonly IClock _clock;

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
        IClock clock)
    {
        _sales = sales;
        _products = products;
        _units = units;
        _customers = customers;
        _credits = credits;
        _dueDateChanges = dueDateChanges;
        _saleStock = saleStock;
        _shifts = shifts;
        _operationalSetups = operationalSetups;
        _clock = clock;
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

            if (lines is null || lines.Count == 0)
            {
                return ApplicationResult<Sale>.Failure(
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

            var drafts = new List<SaleLineDraft>();
            var usesTrustedSnapshots = CheckoutSaleLineSnapshots.RequestUsesTrustedSnapshots(lines);
            if (usesTrustedSnapshots
                && (clientSaleId is null || clientSaleId == Guid.Empty))
            {
                return ApplicationResult<Sale>.Failure(
                    ApplicationErrorCodes.SaleSnapshotInvalid,
                    "Trusted sale line snapshots require a client SaleId (offline sync). Online carts must omit snapshot fields.");
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

            if (usesTrustedSnapshots)
            {
                foreach (var line in lines)
                {
                    if (line is null)
                    {
                        continue;
                    }

                    if (!byId.TryGetValue(line.ProductId, out var product))
                    {
                        return ApplicationResult<Sale>.Failure(
                            ApplicationErrorCodes.SaleProductNotFound,
                            "One or more products in the cart were not found in this organization.");
                    }

                    if (product.Status != CatalogProductStatus.Active)
                    {
                        return ApplicationResult<Sale>.Failure(
                            ApplicationErrorCodes.SaleProductNotActive,
                            $"'{product.Name}' is inactive and cannot be sold. Remove it from the cart or reactivate it.");
                    }

                    CatalogProductUnit? sellingUnit = null;
                    if (line.SellingUnitId is not null)
                    {
                        unitsById.TryGetValue(line.SellingUnitId.Value, out sellingUnit);
                    }

                    var snapshotDraft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, product, sellingUnit);
                    if (!snapshotDraft.IsSuccess)
                    {
                        return ApplicationResult<Sale>.Failure(
                            snapshotDraft.ErrorCode!,
                            snapshotDraft.ErrorMessage!);
                    }

                    drafts.Add(snapshotDraft.Value!);
                }

                if (drafts.Count == 0)
                {
                    return ApplicationResult<Sale>.Failure(
                        DomainErrorCodes.SaleRequiresAtLeastOneLine,
                        "A sale must contain at least one line.");
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
                            return ApplicationResult<Sale>.Failure(
                                ApplicationErrorCodes.SaleProductNotFound,
                                "One or more products in the cart were not found in this organization.");
                        }

                        if (product.Status != CatalogProductStatus.Active)
                        {
                            return ApplicationResult<Sale>.Failure(
                                ApplicationErrorCodes.SaleProductNotActive,
                                $"'{product.Name}' is inactive and cannot be sold. Remove it from the cart or reactivate it.");
                        }

                        CatalogProductUnit? sellingUnit = null;
                        if (line.SellingUnitId is not null)
                        {
                            unitsById.TryGetValue(line.SellingUnitId.Value, out sellingUnit);
                        }

                        var onlineDraft = CheckoutSaleLineSnapshots.TryCreateOnlineDraft(line, product, sellingUnit);
                        if (!onlineDraft.IsSuccess)
                        {
                            return ApplicationResult<Sale>.Failure(onlineDraft.ErrorCode!, onlineDraft.ErrorMessage!);
                        }

                        drafts.Add(onlineDraft.Value!);
                    }
                }
                else
                {
                    var requested = CombineRequestedQuantities(lines);
                    if (requested.Count == 0)
                    {
                        return ApplicationResult<Sale>.Failure(
                            DomainErrorCodes.SaleRequiresAtLeastOneLine,
                            "A sale must contain at least one line.");
                    }

                    drafts = new List<SaleLineDraft>(requested.Count);
                    foreach (var (productId, quantity) in requested)
                    {
                        if (!byId.TryGetValue(productId, out var product))
                        {
                            return ApplicationResult<Sale>.Failure(
                                ApplicationErrorCodes.SaleProductNotFound,
                                "One or more products in the cart were not found in this organization.");
                        }

                        if (product.Status != CatalogProductStatus.Active)
                        {
                            return ApplicationResult<Sale>.Failure(
                                ApplicationErrorCodes.SaleProductNotActive,
                                $"'{product.Name}' is inactive and cannot be sold. Remove it from the cart or reactivate it.");
                        }

                        drafts.Add(new SaleLineDraft(
                            product.Id,
                            product.Name,
                            product.Sku,
                            product.Barcode,
                            product.UnitOfMeasure,
                            product.SellingPrice,
                            quantity,
                            product.SellingMode));
                    }
                }

                if (drafts.Count == 0)
                {
                    return ApplicationResult<Sale>.Failure(
                        DomainErrorCodes.SaleRequiresAtLeastOneLine,
                        "A sale must contain at least one line.");
                }
            }

            var utcNow = _clock.UtcNow;
            var capturedCustomerId = linkedCustomerId;
            var capturedCreditEntryId = linkedCreditEntryId;
            var capturedDueDate = dueDate;
            var capturedActorId = actorId;
            var productsById = byId;

            var previewSubtotal = SaleMoney.RoundMoney(
                drafts.Sum(d => SaleMoney.RoundMoney(d.UnitPrice * (d.EnteredQuantity ?? d.Quantity))));

            decimal taxAmount = 0;
            TaxPricingMode? taxPricingMode = null;
            var setup = await _operationalSetups
                .GetByOrganizationIdAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
            if (setup is { IsCompleted: true } && setup.TaxRatePercent > 0)
            {
                taxPricingMode = setup.TaxPricingMode;
                taxAmount = OperationalSetupTaxCalculator.ComputeTaxAmount(
                    previewSubtotal,
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
                        resolvedBuyerParty),
                    async (createdSale, ct) =>
                    {
                        // Electronic Card/GCash sales await payment — stock deducts only after Paid webhook.
                        if (isElectronic)
                        {
                            return;
                        }

                        await _saleStock
                            .DeductForSaleAsync(orgId, createdSale, productsById, capturedActorId, utcNow, ct)
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
    private readonly ICreditEntryRepository _credits;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly ISaleStockService _saleStock;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public VoidSale(
        ISaleRepository sales,
        ICreditEntryRepository credits,
        IOutstandingBalanceService outstanding,
        ISaleStockService saleStock,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _sales = sales;
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
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
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

                    await _saleStock
                        .RestoreForSaleVoidAsync(orgId, current, actorId, reason, _clock.UtcNow, ct)
                        .ConfigureAwait(false);

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return ApplicationResult<Sale>.Success(current);
                }, cancellationToken)
                .ConfigureAwait(false);
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
