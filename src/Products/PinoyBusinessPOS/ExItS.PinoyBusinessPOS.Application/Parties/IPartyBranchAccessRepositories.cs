using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Parties;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.Parties;

public interface ICustomerBranchAccessRepository
{
    Task<bool> HasAccessAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<POSCustomerId>> ListAccessibleCustomerIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<POSCustomerId>> FilterAccessibleCustomerIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<POSCustomerId> customerIds,
        CancellationToken cancellationToken = default);

    Task GrantAsync(
        CustomerBranchAccess access,
        CancellationToken cancellationToken = default);
}

public interface ISupplierBranchAccessRepository
{
    Task<bool> HasAccessAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        SupplierId supplierId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierId>> ListAccessibleSupplierIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierId>> FilterAccessibleSupplierIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<SupplierId> supplierIds,
        CancellationToken cancellationToken = default);

    Task GrantAsync(
        SupplierBranchAccess access,
        CancellationToken cancellationToken = default);
}
