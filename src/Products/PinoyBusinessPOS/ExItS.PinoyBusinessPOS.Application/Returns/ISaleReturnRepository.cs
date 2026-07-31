using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

public sealed record SaleReturnFilter(Guid? SaleId = null, string? ReturnNumber = null);

public sealed record SaleLineReturnTotals(decimal ReturnedQuantity, decimal RefundedAmount);

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

    Task<SaleReturn> CreateAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, SaleReturn> createReturn,
        Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null,
        CancellationToken cancellationToken = default);
}
