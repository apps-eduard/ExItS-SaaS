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

/// <summary>Header totals for a reporting period without loading sale lines.</summary>
public sealed record SalePeriodAggregate(
    decimal CompletedTotal,
    int CompletedCount,
    decimal VoidedTotal,
    int VoidedCount,
    decimal CashTotal,
    decimal ManualGCashTotal,
    decimal UtangTotal,
    int UtangCount);

public sealed record SalePaymentAggregate(string PaymentMethod, decimal Total, int Count);

public sealed record SaleDailyAggregate(DateOnly Day, decimal Amount, int Count);

/// <summary>Completed-sale COGS aggregates for profitability reporting (excludes voided sales).</summary>
public sealed record SaleCostPeriodAggregate(
    int CompletedCount,
    int CompleteCostCount,
    int PartialCostCount,
    int UnavailableCostCount,
    decimal KnownCogsSum);

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
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns sale IDs from <paramref name="saleIds"/> that belong to <paramref name="branchId"/>
    /// within the organization (one query; used for return report branch truth via original sale).
    /// </summary>
    Task<IReadOnlySet<Guid>> ListSaleIdsInBranchAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> saleIds,
        Guid branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes period sale header totals in SQL (no sale lines loaded).
    /// </summary>
    Task<SalePeriodAggregate> AggregatePeriodAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        SaleStatus? status = null,
        SalePaymentMethod? paymentMethod = null,
        Guid? customerId = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completed-sale payment breakdown in SQL for the inclusive UTC date range.
    /// </summary>
    Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completed-sale daily totals in SQL for the inclusive UTC date range.
    /// </summary>
    Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completed-sale COGS header aggregates for profitability (voided sales excluded).
    /// Legacy null <c>cost_status</c> counts as Unavailable.
    /// </summary>
    Task<SaleCostPeriodAggregate> AggregateCostForProfitabilityAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
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

    /// <summary>
    /// Persists a sale and its lines within the caller's transaction (no nested checkout transaction).
    /// </summary>
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Allocates the next organization business-date sale number inside the caller's transaction.
    /// </summary>
    Task<string> ReserveNextSaleNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>True when the sale has one or more completed returns (blocks void).</summary>
    Task<bool> HasReturnsForSaleAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);
}
