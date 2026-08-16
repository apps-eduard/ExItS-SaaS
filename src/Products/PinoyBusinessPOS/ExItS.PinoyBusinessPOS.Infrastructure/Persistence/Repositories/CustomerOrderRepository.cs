using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CustomerOrderRepository : ICustomerOrderRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public CustomerOrderRepository(PosDbContext db) => _db = db;

    public async Task<CustomerOrder?> GetByIdAsync(
        PosOrganizationId sellerOrganizationId,
        CustomerOrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CustomerOrders.AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.Id == orderId.Value && o.SellerOrganizationId == sellerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], sellerOrganizationId, cancellationToken).ConfigureAwait(false);
        return CustomerOrderEntityMapper.ToDomain(
            record,
            lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<CustomerOrder?> FindByIdempotencyKeyAsync(
        PosOrganizationId sellerOrganizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var record = await _db.CustomerOrders.AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.SellerOrganizationId == sellerOrganizationId.Value && o.IdempotencyKey == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], sellerOrganizationId, cancellationToken).ConfigureAwait(false);
        return CustomerOrderEntityMapper.ToDomain(
            record,
            lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<CustomerOrder> Items, int TotalCount)> ListAsync(
        PosOrganizationId sellerOrganizationId,
        CustomerOrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CustomerOrders.AsNoTracking()
            .Where(o => o.SellerOrganizationId == sellerOrganizationId.Value);

        if (filter.Status is not null)
        {
            var status = filter.Status.Value.ToString();
            query = query.Where(o => o.Status == status);
        }

        if (filter.FulfillmentType is not null)
        {
            var type = filter.FulfillmentType.Value.ToString();
            query = query.Where(o => o.FulfillmentType == type);
        }

        if (filter.FulfillmentBranchId is Guid branchId)
        {
            query = query.Where(o => o.FulfillmentBranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(filter.OrderNumber))
        {
            var term = filter.OrderNumber.Trim().ToUpperInvariant();
            query = query.Where(o => o.OrderNumber.Contains(term));
        }

        if (filter.CustomerPlatformUserId is Guid userId)
        {
            query = query.Where(o => o.CustomerPlatformUserId == userId);
        }

        if (filter.CustomerBuyerOrganizationId is Guid buyerOrgId)
        {
            query = query.Where(o => o.CustomerBuyerOrganizationId == buyerOrgId);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.OrderNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var lines = await LoadLinesAsync(
                records.Select(r => r.Id).ToList(),
                sellerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        var orders = records
            .Select(r => CustomerOrderEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (orders, total);
    }

    public async Task<(IReadOnlyList<CustomerOrder> Items, int TotalCount)> ListForCustomerPartyAsync(
        CustomerPartyType partyType,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var partyCode = partyType.ToString();
        var query = _db.CustomerOrders.AsNoTracking()
            .Where(o => o.CustomerPartyType == partyCode);

        if (partyType == CustomerPartyType.Personal)
        {
            if (platformUserId is null || platformUserId == Guid.Empty)
            {
                return ([], 0);
            }

            query = query.Where(o => o.CustomerPlatformUserId == platformUserId);
        }
        else
        {
            if (buyerOrganizationId is null || buyerOrganizationId == Guid.Empty)
            {
                return ([], 0);
            }

            query = query.Where(o => o.CustomerBuyerOrganizationId == buyerOrganizationId);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.OrderNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var lines = await _db.CustomerOrderLines.AsNoTracking()
            .Where(l => records.Select(r => r.Id).Contains(l.OrderId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var byOrder = lines.GroupBy(l => l.OrderId).ToDictionary(g => g.Key, g => g.ToList());

        var orders = records
            .Select(r => CustomerOrderEntityMapper.ToDomain(r, byOrder.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (orders, total);
    }

    public async Task<CustomerOrder?> GetForCustomerPartyAsync(
        CustomerOrderId orderId,
        CustomerPartyType partyType,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var partyCode = partyType.ToString();
        var query = _db.CustomerOrders.AsNoTracking()
            .Where(o => o.Id == orderId.Value && o.CustomerPartyType == partyCode);

        if (partyType == CustomerPartyType.Personal)
        {
            if (platformUserId is null || platformUserId == Guid.Empty)
            {
                return null;
            }

            query = query.Where(o => o.CustomerPlatformUserId == platformUserId);
        }
        else
        {
            if (buyerOrganizationId is null || buyerOrganizationId == Guid.Empty)
            {
                return null;
            }

            query = query.Where(o => o.CustomerBuyerOrganizationId == buyerOrganizationId);
        }

        var record = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.CustomerOrderLines.AsNoTracking()
            .Where(l => l.OrderId == record.Id)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return CustomerOrderEntityMapper.ToDomain(record, lines);
    }

    public async Task<CustomerOrder> PlaceAsync(
        PosOrganizationId sellerOrganizationId,
        Func<string, CustomerOrder> createOrder,
        Func<CustomerOrder, CancellationToken, Task>? afterCreated = null,
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompletePlaceAsync(sellerOrganizationId, createOrder, afterCreated, cancellationToken)
                .ConfigureAwait(false);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var order = await CompletePlaceAsync(
                        sellerOrganizationId,
                        createOrder,
                        afterCreated,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return order;
            }
            catch (DbUpdateException ex) when (IsOrderNumberConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.CustomerOrderNumberConflict,
                    "A customer order number was allocated concurrently. Retry place.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<CustomerOrder> CompletePlaceAsync(
        PosOrganizationId sellerOrganizationId,
        Func<string, CustomerOrder> createOrder,
        Func<CustomerOrder, CancellationToken, Task>? afterCreated,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(sellerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            var order = createOrder(CustomerOrderNumbers.Format(sequence));

            _db.CustomerOrders.Add(CustomerOrderEntityMapper.ToRecord(order));
            foreach (var line in order.Lines)
            {
                _db.CustomerOrderLines.Add(
                    CustomerOrderEntityMapper.ToRecord(line, order.SellerOrganizationId.Value));
            }

            if (afterCreated is not null)
            {
                await afterCreated(order, cancellationToken).ConfigureAwait(false);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return order;
        }
        catch (DbUpdateException ex) when (IsOrderNumberConflict(ex))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CustomerOrderNumberConflict,
                "A customer order number was allocated concurrently. Retry place.");
        }
    }

    public async Task UpdateAsync(CustomerOrder order, CancellationToken cancellationToken = default)
    {
        var record = await _db.CustomerOrders
            .FirstOrDefaultAsync(
                o => o.Id == order.Id.Value && o.SellerOrganizationId == order.SellerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CustomerOrderNotFound,
                "Customer order was not found.");
        }

        CustomerOrderEntityMapper.ApplyToRecord(order, record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<long> ReserveNextSequenceAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId)], cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.CustomerOrderNumberSequences
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (sequence is null)
        {
            _db.CustomerOrderNumberSequences.Add(new CustomerOrderNumberSequenceRecord
            {
                OrganizationId = organizationId.Value,
                LastValue = 1
            });
            return 1;
        }

        sequence.LastValue += 1;
        return sequence.LastValue;
    }

    private static long SequenceLockKey(PosOrganizationId organizationId)
    {
        Span<byte> bytes = stackalloc byte[16];
        organizationId.Value.TryWriteBytes(bytes);
        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            // Distinct namespace from sale/PO locks.
            return (long)(hash ^ 0xC0FFEEC0FFEEL);
        }
    }

    private async Task<Dictionary<Guid, List<CustomerOrderLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> orderIds,
        PosOrganizationId sellerOrganizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.CustomerOrderLines.AsNoTracking()
            .Where(l => l.SellerOrganizationId == sellerOrganizationId.Value && orderIds.Contains(l.OrderId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.OrderId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static bool IsOrderNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && (pg.ConstraintName ?? string.Empty).Contains(
            "ux_customer_orders_org_order_number",
            StringComparison.OrdinalIgnoreCase);
}
