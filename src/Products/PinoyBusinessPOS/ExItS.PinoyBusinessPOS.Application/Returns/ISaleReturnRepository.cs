using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

public sealed record SaleReturnFilter(Guid? SaleId = null, string? ReturnNumber = null);

public sealed record SaleLineReturnTotals(decimal ReturnedQuantity, decimal RefundedAmount);

/// <summary>Return COGS adjustment rows for profitability (links to original sale line costs).</summary>
public sealed record SaleReturnCogsPeriodAggregate(
    decimal KnownReturnCogs,
    bool HasUnknownCostReturn);

/// <summary>
/// Per-product completed-return refund/COGS aggregates using original sale line UnitCostSnapshot.
/// </summary>
public sealed record ProductProfitabilityReturnAggregate(
    Guid ProductId,
    decimal QuantityReturned,
    decimal RefundAmount,
    decimal KnownReturnCogs,
    bool HasUnknownCostReturn);

public interface ISaleReturnRepository
{
    Task<SaleReturn?> GetByIdAsync(
        PosOrganizationId organizationId,
        SaleReturnId returnId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SaleReturn> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SaleReturnFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleReturn>> ListBySaleIdAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasReturnsForSaleAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, SaleLineReturnTotals>> GetPriorTotalsBySaleLineAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumCashRefundsForShiftAsync(
        PosOrganizationId organizationId,
        Guid cashierShiftId,
        CancellationToken cancellationToken = default);

    Task<SaleReturnCogsPeriodAggregate> AggregateReturnCogsForPeriodAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Per-product return refund/COGS aggregates for product profitability (SQL; original sale line costs).
    /// </summary>
    Task<IReadOnlyList<ProductProfitabilityReturnAggregate>> AggregateProductProfitabilityReturnsAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    Task<decimal> SumRefundsForPeriodAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    Task<SaleReturn> CreateAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, SaleReturn> createReturn,
        Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null,
        CancellationToken cancellationToken = default);
}
