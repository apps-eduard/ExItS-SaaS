using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>Sales history filter. Online-only; there is no offline sale cache or queue.</summary>
public sealed record SaleFilter(
    SaleStatus? Status = null,
    SalePaymentMethod? PaymentMethod = null,
    DateOnly? FromDateUtc = null,
    DateOnly? ToDateUtc = null,
    string? SaleNumber = null);

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);

    Task<Sale?> FindBySaleNumberAsync(
        PosOrganizationId organizationId,
        string saleNumber,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SaleFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all organization sales (with lines) whose UTC calendar <c>RecordedAtUtc</c> falls in the
    /// inclusive range. Callers must enforce <see cref="Reporting.PosReportOptions.MaxInclusiveDaySpan"/>.
    /// </summary>
    Task<IReadOnlyList<Sale>> ListForReportAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        SaleStatus? status = null,
        SalePaymentMethod? paymentMethod = null,
        Guid? productId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves the next organization- and business-date-scoped sale number, builds the sale through
    /// <paramref name="createSale"/>, optionally runs <paramref name="afterSaleCreated"/> (e.g. to
    /// attach a Product-Based Utang credit) before a single SaveChanges, then persists everything in
    /// one transaction. Concurrent checkouts therefore never share a sale number.
    /// </summary>
    Task<Sale> CheckoutAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, Sale> createSale,
        Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>True when the sale has one or more completed returns (blocks void).</summary>
    Task<bool> HasReturnsForSaleAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);
}
