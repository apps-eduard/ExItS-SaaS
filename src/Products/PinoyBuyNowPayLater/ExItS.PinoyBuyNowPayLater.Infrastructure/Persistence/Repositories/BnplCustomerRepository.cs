using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Customers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Repositories;

internal sealed class BnplCustomerRepository : IBnplCustomerRepository
{
    private readonly BnplDbContext _db;

    public BnplCustomerRepository(BnplDbContext db) => _db = db;

    public async Task<BnplCustomer?> GetByIdAsync(
        Guid organizationId,
        BnplCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == customerId.Value && c.OrganizationId == organizationId,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : BnplCustomerEntityMapper.ToDomain(record);
    }

    public async Task<BnplCustomer?> FindByLinkedPersonalPublicUserIdAsync(
        Guid organizationId,
        string linkedPersonalPublicUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = linkedPersonalPublicUserId.Trim().ToUpperInvariant();
        var record = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId
                     && c.LinkedPersonalPublicUserId == normalized,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : BnplCustomerEntityMapper.ToDomain(record);
    }

    public async Task<BnplCustomer?> FindByLinkedCommerceCustomerIdAsync(
        Guid organizationId,
        Guid linkedCommerceCustomerId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId
                     && c.LinkedCommerceCustomerId == linkedCommerceCustomerId,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : BnplCustomerEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<BnplCustomer> Items, int TotalCount)> SearchAsync(
        Guid organizationId,
        string? search,
        BnplCustomerStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(c => c.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            var digits = new string(term.Where(char.IsDigit).ToArray());
            query = query.Where(c =>
                c.DisplayName.ToLower().Contains(term)
                || (c.NormalizedEmail != null && c.NormalizedEmail.Contains(term))
                || (c.Email != null && c.Email.ToLower().Contains(term))
                || (digits.Length > 0 && c.NormalizedMobile != null && c.NormalizedMobile.Contains(digits))
                || (c.LinkedPersonalPublicUserId != null
                    && c.LinkedPersonalPublicUserId.ToLower().Contains(term))
                || (c.LinkedCommerceCustomerId != null
                    && c.LinkedCommerceCustomerId.Value.ToString().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(c => c.DisplayName)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(BnplCustomerEntityMapper.ToDomain).ToList(), total);
    }

    public async Task AddAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.Customers.AddAsync(BnplCustomerEntityMapper.ToRecord(customer), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw MapUniqueViolation(ex);
        }
    }

    public async Task UpdateAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
    {
        var record = await _db.Customers
            .FirstOrDefaultAsync(
                c => c.Id == customer.Id.Value && c.OrganizationId == customer.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new BnplPersistenceConflictException(
                BnplCustomerErrorCodes.NotFound,
                "Customer was not found in this organization.");
        }

        BnplCustomerEntityMapper.CopyToRecord(customer, record);
        await Task.CompletedTask;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static BnplPersistenceConflictException MapUniqueViolation(DbUpdateException ex)
    {
        var constraint = (ex.InnerException as PostgresException)?.ConstraintName ?? string.Empty;
        if (constraint.Contains("linked_personal", StringComparison.OrdinalIgnoreCase))
        {
            return new BnplPersistenceConflictException(
                BnplCustomerErrorCodes.PersonalLinkConflict,
                "Another BNPL customer in this organization already links that Platform Personal identity.");
        }

        if (constraint.Contains("linked_commerce", StringComparison.OrdinalIgnoreCase))
        {
            return new BnplPersistenceConflictException(
                BnplCustomerErrorCodes.CommerceLinkConflict,
                "Another BNPL customer in this organization already links that Commerce customer id.");
        }

        return new BnplPersistenceConflictException(
            BnplCustomerErrorCodes.IdempotencyConflict,
            "A unique customer constraint was violated.");
    }
}

internal sealed class BnplUnitOfWork : IBnplUnitOfWork
{
    private readonly BnplDbContext _db;

    public BnplUnitOfWork(BnplDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            var constraint = ((PostgresException)ex.InnerException!).ConstraintName ?? string.Empty;
            if (constraint.Contains("linked_personal", StringComparison.OrdinalIgnoreCase))
            {
                throw new BnplPersistenceConflictException(
                    BnplCustomerErrorCodes.PersonalLinkConflict,
                    "Another BNPL customer in this organization already links that Platform Personal identity.");
            }

            if (constraint.Contains("linked_commerce", StringComparison.OrdinalIgnoreCase))
            {
                throw new BnplPersistenceConflictException(
                    BnplCustomerErrorCodes.CommerceLinkConflict,
                    "Another BNPL customer in this organization already links that Commerce customer id.");
            }

            throw new BnplPersistenceConflictException(
                BnplCustomerErrorCodes.IdempotencyConflict,
                "A unique customer constraint was violated.");
        }
    }
}
