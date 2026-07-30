using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class POSCustomerRepository : IPOSCustomerRepository
{
    private readonly PosDbContext _db;

    public POSCustomerRepository(PosDbContext db) => _db = db;

    public async Task<POSCustomer?> GetByIdAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == customerId.Value && c.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CustomerEntityMapper.ToDomain(record);
    }

    public async Task<POSCustomer?> FindActiveByNormalizedMobileAsync(
        PosOrganizationId organizationId,
        string normalizedMobile,
        CancellationToken cancellationToken = default)
    {
        var active = CustomerStatus.Active.ToString();
        var record = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId.Value
                     && c.NormalizedMobile == normalizedMobile
                     && c.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CustomerEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        CustomerStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value);

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(c => c.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            var normalizedDigits = new string(term.Where(char.IsDigit).ToArray());
            string? phCanonical = null;
            if (normalizedDigits.Length == 11 && normalizedDigits.StartsWith('0'))
            {
                phCanonical = "63" + normalizedDigits[1..];
            }

            query = query.Where(c =>
                c.DisplayName.ToLower().Contains(term)
                || (normalizedDigits.Length > 0
                    && c.NormalizedMobile != null
                    && (c.NormalizedMobile.Contains(normalizedDigits)
                        || (phCanonical != null && c.NormalizedMobile.Contains(phCanonical))))
                || (c.MobileNumber != null && c.MobileNumber.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(c => c.DisplayName)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CustomerEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value);

        if (sinceUtc is not null)
        {
            var since = sinceUtc.Value.ToUniversalTime();
            query = query.Where(c => c.UpdatedAtUtc > since);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(c => c.UpdatedAtUtc)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CustomerEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
    {
        _db.Customers.Add(CustomerEntityMapper.ToRecord(customer));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default)
    {
        var record = await _db.Customers
            .FirstOrDefaultAsync(
                c => c.Id == customer.Id.Value && c.OrganizationId == customer.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        CustomerEntityMapper.ApplyToRecord(customer, record);
    }
}

internal sealed class PosUnitOfWork : IPosUnitOfWork
{
    private readonly PosDbContext _db;

    public PosUnitOfWork(PosDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The record was modified by another request. Reload and try again.");
        }
        catch (DbUpdateException ex) when (PersistenceExceptionMapper.TryMapUniqueViolation(ex, out var errorCode, out var message))
        {
            throw new PersistenceConflictException(errorCode, message);
        }
    }

    public async Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var result = await action(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure
                                               || ex.SqlState == PostgresErrorCodes.DeadlockDetected)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.ConcurrencyConflict,
                    "Concurrent balance activity changed the available outstanding. Reload and try again.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }
}
