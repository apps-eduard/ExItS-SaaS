using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Quotations;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Quotations;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Quotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class QuotationRepository : IQuotationRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public QuotationRepository(PosDbContext db) => _db = db;

    public async Task<Quotation?> GetByIdAsync(
        PosOrganizationId organizationId,
        QuotationId quotationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Quotations.AsNoTracking()
            .FirstOrDefaultAsync(
                q => q.Id == quotationId.Value && q.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return QuotationEntityMapper.ToDomain(record, lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<Quotation> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        QuotationFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Quotations.AsNoTracking()
            .Where(q => q.OrganizationId == organizationId.Value);

        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(q => q.Status == statusName);
        }

        if (filter.CustomerId is not null)
        {
            query = query.Where(q => q.CustomerId == filter.CustomerId.Value);
        }

        if (filter.BranchId is not null)
        {
            query = query.Where(q => q.BranchId == filter.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.QuotationNumber))
        {
            var term = filter.QuotationNumber.Trim().ToUpperInvariant();
            query = query.Where(q => q.QuotationNumber != null && q.QuotationNumber.Contains(term));
        }

        if (filter.FromIssuedDate is not null)
        {
            var from = new DateTimeOffset(filter.FromIssuedDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(q => q.IssuedAtUtc != null && q.IssuedAtUtc >= from);
        }

        if (filter.ToIssuedDate is not null)
        {
            var exclusiveTo = new DateTimeOffset(
                filter.ToIssuedDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(q => q.IssuedAtUtc != null && q.IssuedAtUtc < exclusiveTo);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(q => q.UpdatedAtUtc)
            .ThenByDescending(q => q.CreatedAtUtc)
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
                organizationId,
                cancellationToken)
            .ConfigureAwait(false);

        var items = records
            .Select(r => QuotationEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (items, total);
    }

    public Task AddAsync(Quotation quotation, CancellationToken cancellationToken = default)
    {
        _db.Quotations.Add(QuotationEntityMapper.ToRecord(quotation));
        foreach (var line in quotation.Lines)
        {
            _db.QuotationLines.Add(QuotationEntityMapper.ToRecord(line));
        }

        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Quotation quotation, CancellationToken cancellationToken = default)
    {
        var record = await _db.Quotations
            .FirstOrDefaultAsync(
                q => q.Id == quotation.Id.Value && q.OrganizationId == quotation.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.QuotationNotFound,
                "Quotation was not found.");
        }

        QuotationEntityMapper.ApplyToRecord(quotation, record);

        var existingLines = await _db.QuotationLines
            .Where(l => l.QuotationId == quotation.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.QuotationLines.RemoveRange(existingLines);
        foreach (var line in quotation.Lines)
        {
            _db.QuotationLines.Add(QuotationEntityMapper.ToRecord(line));
        }
    }

    public Task<Quotation> IssueAsync(
        PosOrganizationId organizationId,
        QuotationId quotationId,
        DateOnly businessDateUtc,
        Func<string, Quotation> applyIssue,
        CancellationToken cancellationToken = default) =>
        ExecuteNumberedMutationAsync(
            organizationId,
            businessDateUtc,
            async (number, ct) =>
            {
                var quotation = applyIssue(number);
                var record = await _db.Quotations
                    .FirstOrDefaultAsync(
                        q => q.Id == quotationId.Value && q.OrganizationId == organizationId.Value,
                        ct)
                    .ConfigureAwait(false);
                if (record is null)
                {
                    throw new PersistenceConflictException(
                        ApplicationErrorCodes.QuotationNotFound,
                        "Quotation was not found.");
                }

                QuotationEntityMapper.ApplyToRecord(quotation, record);
                var existingLines = await _db.QuotationLines
                    .Where(l => l.QuotationId == quotation.Id.Value)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                _db.QuotationLines.RemoveRange(existingLines);
                foreach (var line in quotation.Lines)
                {
                    _db.QuotationLines.Add(QuotationEntityMapper.ToRecord(line));
                }

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return quotation;
            },
            cancellationToken);

    private async Task<T> ExecuteNumberedMutationAsync<T>(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, CancellationToken, Task<T>> complete,
        CancellationToken cancellationToken)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteNumberedAsync(organizationId, businessDateUtc, complete, cancellationToken)
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
                var result = await CompleteNumberedAsync(organizationId, businessDateUtc, complete, cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (DbUpdateException ex) when (IsNumberConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.QuotationNumberConflict,
                    "A quotation number was allocated concurrently. Retry the issue.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<T> CompleteNumberedAsync<T>(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, CancellationToken, Task<T>> complete,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, cancellationToken)
                .ConfigureAwait(false);
            return await complete(QuotationNumbers.Format(businessDateUtc, sequence), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsNumberConflict(ex))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.QuotationNumberConflict,
                "A quotation number was allocated concurrently. Retry the issue.");
        }
    }

    private async Task<long> ReserveNextSequenceAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId, businessDateUtc)], cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.QuotationNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        if (sequence is null)
        {
            _db.QuotationNumberSequences.Add(new QuotationNumberSequenceRecord
            {
                OrganizationId = organizationId.Value,
                BusinessDate = businessDateUtc,
                LastValue = 1
            });
            return 1;
        }

        sequence.LastValue += 1;
        return sequence.LastValue;
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = (byte)7; // quotation sequence namespace (distinct from PO/GRN/sale)

        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)hash;
        }
    }

    private async Task<Dictionary<Guid, List<QuotationLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> quotationIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.QuotationLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && quotationIds.Contains(l.QuotationId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.QuotationId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static bool IsNumberConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            return false;
        }

        var constraint = pg.ConstraintName ?? string.Empty;
        return constraint.Contains("ux_quotations_org_quotation_number", StringComparison.OrdinalIgnoreCase);
    }
}
