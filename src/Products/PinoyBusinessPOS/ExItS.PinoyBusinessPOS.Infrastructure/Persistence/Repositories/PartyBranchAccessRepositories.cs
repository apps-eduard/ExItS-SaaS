using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Parties;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Parties;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CustomerBranchAccessRepository : ICustomerBranchAccessRepository
{
    private readonly PosDbContext _db;

    public CustomerBranchAccessRepository(PosDbContext db) => _db = db;

    public Task<bool> HasAccessAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default) =>
        _db.CustomerBranchAccess.AsNoTracking()
            .AnyAsync(
                a => a.OrganizationId == organizationId.Value
                    && a.BranchId == branchId.Value
                    && a.CustomerId == customerId.Value,
                cancellationToken);

    public async Task<IReadOnlyList<POSCustomerId>> ListAccessibleCustomerIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var ids = await _db.CustomerBranchAccess.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value && a.BranchId == branchId.Value)
            .Select(a => a.CustomerId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ids.Select(POSCustomerId.From).ToList();
    }

    public async Task<IReadOnlyList<POSCustomerId>> FilterAccessibleCustomerIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<POSCustomerId> customerIds,
        CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
        {
            return [];
        }

        var wanted = customerIds.Select(id => id.Value).ToList();
        var ids = await _db.CustomerBranchAccess.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value
                && a.BranchId == branchId.Value
                && wanted.Contains(a.CustomerId))
            .Select(a => a.CustomerId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ids.Select(POSCustomerId.From).ToList();
    }

    public async Task GrantAsync(CustomerBranchAccess access, CancellationToken cancellationToken = default)
    {
        var record = PartyBranchAccessEntityMapper.ToRecord(access);
        var exists = await _db.CustomerBranchAccess.AsNoTracking()
            .AnyAsync(
                a => a.OrganizationId == record.OrganizationId
                    && a.BranchId == record.BranchId
                    && a.CustomerId == record.CustomerId
                    && a.GrantSource == record.GrantSource,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        _db.CustomerBranchAccess.Add(record);
    }

    public async Task RevokeGrantAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        POSCustomerId customerId,
        PartyBranchGrantSource grantSource,
        CancellationToken cancellationToken = default)
    {
        var sourceCode = PartyBranchGrantSources.ToCode(grantSource);
        await _db.CustomerBranchAccess
            .Where(a => a.OrganizationId == organizationId.Value
                && a.BranchId == branchId.Value
                && a.CustomerId == customerId.Value
                && a.GrantSource == sourceCode)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

internal sealed class SupplierBranchAccessRepository : ISupplierBranchAccessRepository
{
    private readonly PosDbContext _db;

    public SupplierBranchAccessRepository(PosDbContext db) => _db = db;

    public Task<bool> HasAccessAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        SupplierId supplierId,
        CancellationToken cancellationToken = default) =>
        _db.SupplierBranchAccess.AsNoTracking()
            .AnyAsync(
                a => a.OrganizationId == organizationId.Value
                    && a.BranchId == branchId.Value
                    && a.SupplierId == supplierId.Value,
                cancellationToken);

    public async Task<IReadOnlyList<SupplierId>> ListAccessibleSupplierIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var ids = await _db.SupplierBranchAccess.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value && a.BranchId == branchId.Value)
            .Select(a => a.SupplierId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ids.Select(SupplierId.From).ToList();
    }

    public async Task<IReadOnlyList<SupplierId>> FilterAccessibleSupplierIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<SupplierId> supplierIds,
        CancellationToken cancellationToken = default)
    {
        if (supplierIds.Count == 0)
        {
            return [];
        }

        var wanted = supplierIds.Select(id => id.Value).ToList();
        var ids = await _db.SupplierBranchAccess.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value
                && a.BranchId == branchId.Value
                && wanted.Contains(a.SupplierId))
            .Select(a => a.SupplierId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ids.Select(SupplierId.From).ToList();
    }

    public async Task GrantAsync(SupplierBranchAccess access, CancellationToken cancellationToken = default)
    {
        var record = PartyBranchAccessEntityMapper.ToRecord(access);
        var exists = await _db.SupplierBranchAccess.AsNoTracking()
            .AnyAsync(
                a => a.OrganizationId == record.OrganizationId
                    && a.BranchId == record.BranchId
                    && a.SupplierId == record.SupplierId
                    && a.GrantSource == record.GrantSource,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        _db.SupplierBranchAccess.Add(record);
    }

    public async Task RevokeGrantAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        SupplierId supplierId,
        PartyBranchGrantSource grantSource,
        CancellationToken cancellationToken = default)
    {
        var sourceCode = PartyBranchGrantSources.ToCode(grantSource);
        await _db.SupplierBranchAccess
            .Where(a => a.OrganizationId == organizationId.Value
                && a.BranchId == branchId.Value
                && a.SupplierId == supplierId.Value
                && a.GrantSource == sourceCode)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
