using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Registers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class RegisterRepository : IRegisterRepository
{
    /// <summary>
    /// Transaction-scoped advisory lock over the organization register-code counter so concurrent
    /// creates cannot interleave the read-modify-write of <c>pos.register_code_sequences</c>.
    /// </summary>
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public RegisterRepository(PosDbContext db) => _db = db;

    public async Task<Register?> GetByIdAsync(
        PosOrganizationId organizationId,
        RegisterId registerId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Registers.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == registerId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RegisterEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<Register> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        RegisterFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Registers.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);

        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(r => r.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(filter.RegisterCode))
        {
            var code = filter.RegisterCode.Trim().ToUpperInvariant();
            query = query.Where(r => r.RegisterCode.Contains(code));
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var term = filter.Name.Trim().ToUpperInvariant();
            query = query.Where(r => r.NormalizedName.Contains(term));
        }

        if (filter.HasOpenShift is not null)
        {
            var openRegisterIds = _db.CashierShifts.AsNoTracking()
                .Where(s => s.OrganizationId == organizationId.Value
                            && s.Status == nameof(CashierShiftStatus.Open)
                            && s.RegisterId != null)
                .Select(s => s.RegisterId!.Value);

            query = filter.HasOpenShift.Value
                ? query.Where(r => openRegisterIds.Contains(r.Id))
                : query.Where(r => !openRegisterIds.Contains(r.Id));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(r => r.RegisterCode)
            .ThenBy(r => r.NormalizedName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(RegisterEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<Register>> ListAvailableForShiftAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var openRegisterIds = await _db.CashierShifts.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value
                        && s.Status == nameof(CashierShiftStatus.Open)
                        && s.RegisterId != null)
            .Select(s => s.RegisterId!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var openSet = openRegisterIds.ToHashSet();
        var active = nameof(RegisterStatus.Active);
        var records = await _db.Registers.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value && r.Status == active)
            .OrderBy(r => r.RegisterCode)
            .ThenBy(r => r.NormalizedName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .Where(r => !openSet.Contains(r.Id))
            .Select(RegisterEntityMapper.ToDomain)
            .ToList();
    }

    public async Task<Register?> FindByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Registers.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.NormalizedName == normalizedName,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RegisterEntityMapper.ToDomain(record);
    }

    public async Task<bool> HasOpenShiftAsync(
        PosOrganizationId organizationId,
        RegisterId registerId,
        CancellationToken cancellationToken = default)
    {
        return await _db.CashierShifts.AsNoTracking()
            .AnyAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.RegisterId == registerId.Value
                     && s.Status == nameof(CashierShiftStatus.Open),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<string> AllocateNextRegisterCodeAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId)], cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.RegisterCodeSequences
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);

        long next;
        if (sequence is null)
        {
            next = 1;
            _db.RegisterCodeSequences.Add(new RegisterCodeSequenceRecord
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

        return RegisterCodes.Format(next);
    }

    public Task AddAsync(Register register, CancellationToken cancellationToken = default)
    {
        _db.Registers.Add(RegisterEntityMapper.ToRecord(register));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Register register, CancellationToken cancellationToken = default)
    {
        var record = await _db.Registers
            .FirstOrDefaultAsync(
                r => r.Id == register.Id.Value && r.OrganizationId == register.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.RegisterNotFound,
                "Register was not found.");
        }

        RegisterEntityMapper.ApplyToRecord(register, record);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId)
    {
        Span<byte> bytes = stackalloc byte[16];
        organizationId.Value.TryWriteBytes(bytes);

        unchecked
        {
            var hash = 0xd17e91a4c82b5f03UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)hash;
        }
    }
}
