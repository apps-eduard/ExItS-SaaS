using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryLotRepository : IInventoryLotRepository
{
    private readonly PosDbContext _db;

    public InventoryLotRepository(PosDbContext db) => _db = db;

    public async Task<InventoryLot?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryLotId lotId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryLots
            .FirstOrDefaultAsync(
                l => l.OrganizationId == organizationId.Value && l.Id == lotId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryEntityMapper.ToDomain(record);
    }

    public async Task<InventoryLot?> FindAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        DateOnly expirationDate,
        string normalizedLotNumber,
        PosBranchId? branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = branchId?.Value;
        var record = await _db.InventoryLots
            .FirstOrDefaultAsync(
                l => l.OrganizationId == organizationId.Value
                    && l.ProductId == productId.Value
                    && l.ExpirationDate == expirationDate
                    && l.NormalizedLotNumber == normalizedLotNumber
                    && l.BranchId == branch,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<InventoryLot>> ListOnHandAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        bool includeDepleted,
        CancellationToken cancellationToken = default)
    {
        var query = OnHandQuery(organizationId, productId, branchId, includeDepleted);
        var records = await query
            .OrderBy(l => l.ExpirationDate)
            .ThenBy(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryEntityMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListPagedAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        bool includeDepleted,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = OnHandQuery(organizationId, productId, branchId, includeDepleted);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(l => l.ExpirationDate)
            .ThenBy(l => l.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(InventoryEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListExpiringPagedAsync(
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        DateOnly expireOnOrBefore,
        DateOnly? expireOnOrAfter,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ExpiringQuery(organizationId, branchId, expireOnOrBefore, expireOnOrAfter, search);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(l => l.ExpirationDate)
            .ThenBy(l => l.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(InventoryEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
        PosOrganizationId organizationId,
        DateOnly today,
        int warningDays,
        CancellationToken cancellationToken = default)
    {
        var onHand = _db.InventoryLots.Where(l =>
            l.OrganizationId == organizationId.Value && l.QuantityOnHand > 0m);
        var expired = await onHand
            .CountAsync(l => l.ExpirationDate < today, cancellationToken)
            .ConfigureAwait(false);
        var nearUntil = today.AddDays(warningDays);
        var near = await onHand
            .CountAsync(
                l => l.ExpirationDate >= today && l.ExpirationDate <= nearUntil,
                cancellationToken)
            .ConfigureAwait(false);
        return (expired, near);
    }

    public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default)
    {
        _db.InventoryLots.Add(InventoryEntityMapper.ToRecord(lot));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryLots
            .FirstOrDefaultAsync(
                l => l.OrganizationId == lot.OrganizationId.Value && l.Id == lot.Id.Value,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Inventory lot {lot.Id} was not found.");
        InventoryEntityMapper.ApplyToRecord(lot, record);
    }

    public Task AddMovementAsync(InventoryLotMovement movement, CancellationToken cancellationToken = default)
    {
        _db.InventoryLotMovements.Add(InventoryEntityMapper.ToRecord(movement));
        return Task.CompletedTask;
    }

    public Task<bool> HasMovementAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        InventoryLotId lotId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default) =>
        _db.InventoryLotMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == sourceId
                && m.LotId == lotId.Value
                && m.MovementType == StockMovementTypes.ToCode(movementType),
            cancellationToken);

    public async Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default)
    {
        var type = StockMovementTypes.ToCode(movementType);
        var records = await _db.InventoryLotMovements
            .Where(m => m.OrganizationId == organizationId.Value
                && m.SourceId == sourceId
                && m.MovementType == type)
            .OrderBy(m => m.RecordedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryEntityMapper.ToDomain).ToList();
    }

    private IQueryable<InventoryLotRecord> OnHandQuery(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        bool includeDepleted)
    {
        var query = _db.InventoryLots.Where(l =>
            l.OrganizationId == organizationId.Value && l.ProductId == productId.Value);
        if (branchId is not null)
        {
            query = query.Where(l => l.BranchId == branchId.Value);
        }

        if (!includeDepleted)
        {
            query = query.Where(l => l.QuantityOnHand > 0m);
        }

        return query;
    }

    private IQueryable<InventoryLotRecord> ExpiringQuery(
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        DateOnly expireOnOrBefore,
        DateOnly? expireOnOrAfter,
        string? search)
    {
        var query = _db.InventoryLots.Where(l =>
            l.OrganizationId == organizationId.Value
            && l.QuantityOnHand > 0m
            && l.ExpirationDate <= expireOnOrBefore);
        if (expireOnOrAfter is { } from)
        {
            query = query.Where(l => l.ExpirationDate >= from);
        }

        if (branchId is not null)
        {
            query = query.Where(l => l.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var productIds = _db.CatalogProducts
                .Where(p => p.OrganizationId == organizationId.Value
                    && (p.Name.Contains(term)
                        || (p.Sku != null && p.Sku.Contains(term))
                        || (p.Barcode != null && p.Barcode.Contains(term))))
                .Select(p => p.Id);
            query = query.Where(l =>
                productIds.Contains(l.ProductId)
                || (l.LotNumber != null && l.LotNumber.Contains(term)));
        }

        return query;
    }
}
