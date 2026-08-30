using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.SupplierPayables;

public sealed record SupplierPayableFilter(
    SupplierId? SupplierId = null,
    SupplierPayableStatus? Status = null,
    bool? OutstandingOnly = null,
    bool? OverdueOnly = null,
    DateOnly? AsOfDate = null);

public interface ISupplierPayableRepository
{
    Task<SupplierPayable?> GetByIdAsync(
        PosOrganizationId organizationId,
        SupplierPayableId payableId,
        CancellationToken cancellationToken = default);

    Task<SupplierPayable?> FindBySourceAsync(
        PosOrganizationId organizationId,
        SupplierPayableSourceType sourceType,
        Guid sourceId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SupplierPayable> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SupplierPayableFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierPayablePayment>> ListPaymentsAsync(
        PosOrganizationId organizationId,
        SupplierPayableId payableId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SupplierPayable payable, CancellationToken cancellationToken = default);

    Task UpdateAsync(SupplierPayable payable, CancellationToken cancellationToken = default);

    Task<SupplierPayableSummaryTotals> GetSupplierSummaryAsync(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}

public sealed record SupplierPayableSummaryTotals(
    decimal OutstandingTotal,
    decimal OverdueTotal,
    int OpenCount);
