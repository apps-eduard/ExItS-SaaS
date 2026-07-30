using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

public sealed class SaleQueryService
{
    private readonly ISaleRepository _sales;

    public SaleQueryService(ISaleRepository sales) => _sales = sales;

    public async Task<PosSaleDto?> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var sale = await _sales
            .GetByIdAsync(PosOrganizationId.From(organizationId), SaleId.From(saleId), cancellationToken)
            .ConfigureAwait(false);
        return sale is null ? null : Map(sale);
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
                    l.UnitPrice,
                    l.Quantity,
                    l.LineTotal))
                .ToList());
}

/// <summary>
/// Records a simple retail sale. The server is authoritative for every monetary value: it reloads
/// each requested product inside the caller's organization, requires it to be Active, and snapshots
/// the current selling price, name, SKU, barcode and unit of measure onto the line. Client-supplied
/// prices or names are never read.
///
/// No stock is deducted, no Utang credit is created, and no tax or discount is applied.
/// </summary>
public sealed class CheckoutSale
{
    private readonly ISaleRepository _sales;
    private readonly ICatalogProductRepository _products;
    private readonly IClock _clock;

    public CheckoutSale(ISaleRepository sales, ICatalogProductRepository products, IClock clock)
    {
        _sales = sales;
        _products = products;
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

            if (lines is null || lines.Count == 0)
            {
                return ApplicationResult<Sale>.Failure(
                    DomainErrorCodes.SaleRequiresAtLeastOneLine,
                    "A sale must contain at least one line.");
            }

            var requested = CombineRequestedQuantities(lines);
            if (requested.Count == 0)
            {
                return ApplicationResult<Sale>.Failure(
                    DomainErrorCodes.SaleRequiresAtLeastOneLine,
                    "A sale must contain at least one line.");
            }

            var productIds = requested.Select(r => CatalogProductId.From(r.ProductId)).ToList();
            var products = await _products
                .ListByIdsAsync(orgId, productIds, cancellationToken)
                .ConfigureAwait(false);
            var byId = products.ToDictionary(p => p.Id.Value);

            var drafts = new List<SaleLineDraft>(requested.Count);
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
                    quantity));
            }

            var utcNow = _clock.UtcNow;

            // The sale number is reserved inside the same transaction that inserts the sale, so a
            // domain rejection raised by the factory rolls the reservation back and leaves no gap.
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
                        clientSaleId is null ? null : SaleId.From(clientSaleId.Value)),
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
/// Voids a recorded sale with a required reason and actor. Voiding does not refund money, restore
/// stock, or touch any Utang record — it only marks the sale as not counted.
/// </summary>
public sealed class VoidSale
{
    private readonly ISaleRepository _sales;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public VoidSale(ISaleRepository sales, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _sales = sales;
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

        var sale = await _sales
            .GetByIdAsync(PosOrganizationId.From(organizationId), SaleId.From(saleId), cancellationToken)
            .ConfigureAwait(false);
        if (sale is null)
        {
            return ApplicationResult<Sale>.Failure(
                ApplicationErrorCodes.SaleNotFound,
                "Sale was not found.");
        }

        try
        {
            sale.Void(reason, actorId, _clock.UtcNow);
            await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
}
