using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Branches;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class BranchSetupProgressRepository : IBranchSetupProgressRepository
{
    private readonly PosDbContext _db;

    public BranchSetupProgressRepository(PosDbContext db) => _db = db;

    public async Task<BranchSetupProgressDto?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchSetupProgress.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.BranchId == branchId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null
            ? null
            : new BranchSetupProgressDto(
                record.LastVisitedStep,
                record.StartedAtUtc,
                record.LastVisitedAtUtc,
                record.CompletedAtUtc);
    }

    public async Task UpsertVisitAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        string? lastVisitedStep,
        DateTimeOffset utcNow,
        bool markCompleted,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchSetupProgress
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.BranchId == branchId.Value,
                cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            record = new BranchSetupProgressRecord
            {
                OrganizationId = organizationId.Value,
                BranchId = branchId.Value,
                StartedAtUtc = utcNow,
            };
            _db.BranchSetupProgress.Add(record);
        }

        record.LastVisitedStep = string.IsNullOrWhiteSpace(lastVisitedStep) ? record.LastVisitedStep : lastVisitedStep.Trim();
        record.LastVisitedAtUtc = utcNow;
        if (markCompleted)
        {
            record.CompletedAtUtc = utcNow;
        }
    }
}

internal sealed class BranchReadinessMetricsRepository : IBranchReadinessMetricsRepository
{
    private readonly PosDbContext _db;

    public BranchReadinessMetricsRepository(PosDbContext db) => _db = db;

    public async Task<BranchReadinessMetrics> GetMetricsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var org = organizationId.Value;
        var branch = branchId.Value;

        var activeProducts = await _db.CatalogProducts.AsNoTracking()
            .CountAsync(p => p.OrganizationId == org && p.Status == "Active", cancellationToken)
            .ConfigureAwait(false);

        var unavailable = await _db.BranchProductAvailabilities.AsNoTracking()
            .CountAsync(
                a => a.OrganizationId == org && a.BranchId == branch && !a.IsOffered,
                cancellationToken)
            .ConfigureAwait(false);
        var offered = Math.Max(0, activeProducts - unavailable);

        var priceOverrides = await _db.BranchProductPriceOverrides.AsNoTracking()
            .CountAsync(o => o.OrganizationId == org && o.BranchId == branch, cancellationToken)
            .ConfigureAwait(false);

        var productsWithStock = await _db.InventoryBranchBalances.AsNoTracking()
            .CountAsync(
                b => b.OrganizationId == org && b.BranchId == branch && b.OnHandQuantity > 0m,
                cancellationToken)
            .ConfigureAwait(false);

        var customerAccess = await _db.CustomerBranchAccess.AsNoTracking()
            .Where(a => a.OrganizationId == org && a.BranchId == branch)
            .Select(a => a.CustomerId)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var supplierAccess = await _db.SupplierBranchAccess.AsNoTracking()
            .Where(a => a.OrganizationId == org && a.BranchId == branch)
            .Select(a => a.SupplierId)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BranchReadinessMetrics(
            activeProducts,
            offered,
            priceOverrides,
            productsWithStock,
            customerAccess,
            supplierAccess);
    }
}
