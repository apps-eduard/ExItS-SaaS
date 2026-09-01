using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class SupplierRepository : ISupplierRepository
{
    /// <summary>
    /// Transaction-scoped advisory lock over the organization supplier-code counter so concurrent
    /// creates cannot interleave the read-modify-write of <c>pos.supplier_code_sequences</c>.
    /// </summary>
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public SupplierRepository(PosDbContext db) => _db = db;

    public async Task<Supplier?> GetByIdAsync(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == supplierId.Value && s.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SupplierEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SupplierFilter filter,
        int skip,
        int take,
        IReadOnlyCollection<Guid>? restrictToSupplierIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Suppliers.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value);

        if (restrictToSupplierIds is not null)
        {
            if (restrictToSupplierIds.Count == 0)
            {
                return ([], 0);
            }

            query = query.Where(s => restrictToSupplierIds.Contains(s.Id));
        }

        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(s => s.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(filter.SupplierCode))
        {
            var code = filter.SupplierCode.Trim().ToUpperInvariant();
            query = query.Where(s => s.SupplierCode.Contains(code));
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var term = filter.Name.Trim().ToUpperInvariant();
            query = query.Where(s => s.NormalizedName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.ContactPerson))
        {
            var term = filter.ContactPerson.Trim().ToUpperInvariant();
            query = query.Where(s => s.ContactPerson != null && s.ContactPerson.ToUpper().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            var term = filter.Email.Trim().ToUpperInvariant();
            query = query.Where(s => s.NormalizedEmail != null && s.NormalizedEmail.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Mobile))
        {
            var term = filter.Mobile.Trim().ToUpperInvariant();
            query = query.Where(s =>
                (s.NormalizedMobile != null && s.NormalizedMobile.Contains(term))
                || (s.MobileNumber != null && s.MobileNumber.ToUpper().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.TaxOrRegistrationNumber))
        {
            var term = filter.TaxOrRegistrationNumber.Trim().ToUpperInvariant();
            query = query.Where(s =>
                s.NormalizedTaxOrRegistrationNumber != null
                && s.NormalizedTaxOrRegistrationNumber.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.SupplierCode)
            .ThenBy(s => s.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SupplierEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<Supplier?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var active = SupplierStatus.Active.ToString();
        var record = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.NormalizedName == normalizedName
                     && s.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SupplierEntityMapper.ToDomain(record);
    }

    public async Task<Supplier?> FindActiveByNormalizedEmailAsync(
        PosOrganizationId organizationId,
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var active = SupplierStatus.Active.ToString();
        var record = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.NormalizedEmail == normalizedEmail
                     && s.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SupplierEntityMapper.ToDomain(record);
    }

    public async Task<Supplier?> FindActiveByNormalizedMobileAsync(
        PosOrganizationId organizationId,
        string normalizedMobile,
        CancellationToken cancellationToken = default)
    {
        var active = SupplierStatus.Active.ToString();
        var record = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.NormalizedMobile == normalizedMobile
                     && s.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SupplierEntityMapper.ToDomain(record);
    }

    public async Task<Supplier?> FindActiveByNormalizedTaxAsync(
        PosOrganizationId organizationId,
        string normalizedTax,
        CancellationToken cancellationToken = default)
    {
        var active = SupplierStatus.Active.ToString();
        var record = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.NormalizedTaxOrRegistrationNumber == normalizedTax
                     && s.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SupplierEntityMapper.ToDomain(record);
    }

    public async Task<string> AllocateNextSupplierCodeAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId)], cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.SupplierCodeSequences
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);

        long next;
        if (sequence is null)
        {
            next = 1;
            _db.SupplierCodeSequences.Add(new SupplierCodeSequenceRecord
            {
                OrganizationId = organizationId.Value,
                NextValue = 2
            });
        }
        else
        {
            next = sequence.NextValue;
            sequence.NextValue = next + 1;
        }

        return SupplierCodes.Format(next);
    }

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _db.Suppliers.Add(SupplierEntityMapper.ToRecord(supplier));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        var record = await _db.Suppliers
            .FirstOrDefaultAsync(
                s => s.Id == supplier.Id.Value && s.OrganizationId == supplier.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.SupplierNotFound,
                "Supplier was not found.");
        }

        SupplierEntityMapper.ApplyToRecord(supplier, record);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> supplierIds,
        CancellationToken cancellationToken = default)
    {
        if (supplierIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var idList = supplierIds.Distinct().ToList();
        var rows = await _db.Suppliers.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value && idList.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(r => r.Id, r => r.Name);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId)
    {
        Span<byte> bytes = stackalloc byte[16];
        organizationId.Value.TryWriteBytes(bytes);

        unchecked
        {
            var hash = 0xb5f0a1c9e37d4821UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)hash;
        }
    }
}
