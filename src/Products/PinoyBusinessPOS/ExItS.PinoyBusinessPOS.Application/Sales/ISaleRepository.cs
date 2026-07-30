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
    /// Reserves the next organization- and business-date-scoped sale number, builds the sale through
    /// <paramref name="createSale"/>, then persists the sale, its lines, and the bumped sequence in a
    /// single transaction. Concurrent checkouts therefore never share a sale number.
    /// </summary>
    Task<Sale> CheckoutAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, Sale> createSale,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
}
