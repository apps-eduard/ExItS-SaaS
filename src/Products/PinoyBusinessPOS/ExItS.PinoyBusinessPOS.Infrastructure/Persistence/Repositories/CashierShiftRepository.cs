using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CashierShifts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CashierShiftRepository : ICashierShiftRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public CashierShiftRepository(PosDbContext db) => _db = db;

    public async Task<CashierShift?> GetByIdAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CashierShifts.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == shiftId.Value && s.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CashierShiftEntityMapper.ToDomain(record);
    }

    public async Task<CashierShift?> FindOpenForActorAsync(
        PosOrganizationId organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CashierShifts.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.ActorId == actorId
                     && s.Status == nameof(CashierShiftStatus.Open),
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CashierShiftEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<CashierShift> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        CashierShiftFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CashierShifts.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value);

        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(s => s.Status == statusName);
        }

        if (filter.ActorId is not null)
        {
            query = query.Where(s => s.ActorId == filter.ActorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ShiftNumber))
        {
            var term = filter.ShiftNumber.Trim().ToUpperInvariant();
            query = query.Where(s => s.ShiftNumber.Contains(term));
        }

        if (filter.FromBusinessDate is not null)
        {
            query = query.Where(s => s.BusinessDate >= filter.FromBusinessDate.Value);
        }

        if (filter.ToBusinessDate is not null)
        {
            query = query.Where(s => s.BusinessDate <= filter.ToBusinessDate.Value);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.OpenedAtUtc)
            .ThenByDescending(s => s.ShiftNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CashierShiftEntityMapper.ToDomain).ToList(), total);
    }

    public Task<CashierShift> OpenAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Guid actorId,
        decimal openingCashAmount,
        Guid openedBy,
        Func<string, CashierShift> createShift,
        CancellationToken cancellationToken = default) =>
        ExecuteNumberedMutationAsync(
            organizationId,
            businessDateUtc,
            number =>
            {
                var shift = createShift(number);
                _db.CashierShifts.Add(CashierShiftEntityMapper.ToRecord(shift));
                return shift;
            },
            ApplicationErrorCodes.CashierShiftNumberConflict,
            "A shift number was allocated concurrently. Retry the open.",
            ApplicationErrorCodes.CashierShiftOpenConflict,
            "This cashier already has an open shift.",
            cancellationToken);

    public async Task UpdateAsync(CashierShift shift, CancellationToken cancellationToken = default)
    {
        var record = await _db.CashierShifts
            .FirstOrDefaultAsync(
                s => s.Id == shift.Id.Value && s.OrganizationId == shift.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CashierShiftNotFound,
                "Cashier shift was not found.");
        }

        CashierShiftEntityMapper.ApplyToRecord(shift, record);
    }

    public async Task AddMovementAsync(CashierShiftMovement movement, CancellationToken cancellationToken = default)
    {
        try
        {
            _db.CashierShiftMovements.Add(CashierShiftEntityMapper.ToRecord(movement));
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsMovementConflict(ex))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CashierShiftMovementConflict,
                "The movement id is already in use.");
        }
    }

    public async Task<CashierShiftMovement?> GetMovementByIdAsync(
        PosOrganizationId organizationId,
        CashierShiftMovementId movementId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CashierShiftMovements.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == movementId.Value && m.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CashierShiftEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<CashierShiftMovement>> ListMovementsAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.CashierShiftMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value && m.ShiftId == shiftId.Value)
            .OrderBy(m => m.RecordedAtUtc)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CashierShiftEntityMapper.ToDomain).ToList();
    }

    public Task<bool> HasLinkedSalesAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default) =>
        _db.Sales.AsNoTracking()
            .AnyAsync(
                s => s.OrganizationId == organizationId.Value && s.CashierShiftId == shiftId.Value,
                cancellationToken);

    public async Task<CashierShiftSalesTotals> GetSalesTotalsAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default)
    {
        var sales = await _db.Sales.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value && s.CashierShiftId == shiftId.Value)
            .Select(s => new { s.PaymentMethod, s.Status, s.Total })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cashCode = SalePaymentMethods.ToCode(SalePaymentMethod.Cash);
        var gcashCode = SalePaymentMethods.ToCode(SalePaymentMethod.ManualGCash);
        var utangCode = SalePaymentMethods.ToCode(SalePaymentMethod.Utang);
        var completed = nameof(SaleStatus.Completed);
        var voided = nameof(SaleStatus.Voided);

        var completedCash = sales.Where(s => s.PaymentMethod == cashCode && s.Status == completed).ToList();
        var voidedCash = sales.Where(s => s.PaymentMethod == cashCode && s.Status == voided).ToList();
        var completedGCash = sales.Where(s => s.PaymentMethod == gcashCode && s.Status == completed).ToList();
        var completedUtang = sales.Where(s => s.PaymentMethod == utangCode && s.Status == completed).ToList();

        var completedCashTotal = completedCash.Sum(s => s.Total);
        var voidedCashTotal = voidedCash.Sum(s => s.Total);

        var cashCodeForRefund = SalePaymentMethods.ToCode(SalePaymentMethod.Cash);
        var cashRefunds = await _db.SaleReturns.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value
                        && r.CashierShiftId == shiftId.Value
                        && r.RefundMethod == cashCodeForRefund)
            .SumAsync(r => r.TotalRefundAmount, cancellationToken)
            .ConfigureAwait(false);

        return new CashierShiftSalesTotals(
            SaleMoney.RoundMoney(completedCashTotal - voidedCashTotal),
            SaleMoney.RoundMoney(completedCashTotal),
            SaleMoney.RoundMoney(completedGCash.Sum(s => s.Total)),
            SaleMoney.RoundMoney(completedUtang.Sum(s => s.Total)),
            SaleMoney.RoundMoney(cashRefunds),
            completedCash.Count,
            voidedCash.Count,
            completedGCash.Count,
            completedUtang.Count);
    }

    private async Task<CashierShift> ExecuteNumberedMutationAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, CashierShift> apply,
        string numberConflictCode,
        string numberConflictMessage,
        string openConflictCode,
        string openConflictMessage,
        CancellationToken cancellationToken)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteNumberedMutationAsync(
                    organizationId,
                    businessDateUtc,
                    apply,
                    numberConflictCode,
                    numberConflictMessage,
                    openConflictCode,
                    openConflictMessage,
                    cancellationToken)
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
                var shift = await CompleteNumberedMutationAsync(
                        organizationId,
                        businessDateUtc,
                        apply,
                        numberConflictCode,
                        numberConflictMessage,
                        openConflictCode,
                        openConflictMessage,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return shift;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<CashierShift> CompleteNumberedMutationAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, CashierShift> apply,
        string numberConflictCode,
        string numberConflictMessage,
        string openConflictCode,
        string openConflictMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, cancellationToken)
                .ConfigureAwait(false);
            var shift = apply(CashierShiftNumbers.Format(businessDateUtc, sequence));
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return shift;
        }
        catch (DbUpdateException ex) when (IsOpenShiftConflict(ex))
        {
            throw new PersistenceConflictException(openConflictCode, openConflictMessage);
        }
        catch (DbUpdateException ex) when (IsShiftNumberConflict(ex))
        {
            throw new PersistenceConflictException(numberConflictCode, numberConflictMessage);
        }
    }

    private async Task<long> ReserveNextSequenceAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken)
    {
        await _db.Database
            .ExecuteSqlRawAsync(
                LockSequenceSql,
                [SequenceLockKey(organizationId, businessDateUtc)],
                cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.CashierShiftNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);

        if (sequence is null)
        {
            _db.CashierShiftNumberSequences.Add(new CashierShiftNumberSequenceRecord
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
        Span<byte> bytes = stackalloc byte[20];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..], businessDateUtc.DayNumber);

        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)(hash ^ 0x5348494654UL);
        }
    }

    private static bool IsShiftNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && (pg.ConstraintName ?? string.Empty).Contains("ux_cashier_shifts_org_shift_number", StringComparison.OrdinalIgnoreCase);

    private static bool IsOpenShiftConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && (pg.ConstraintName ?? string.Empty).Contains("ux_cashier_shifts_org_actor_open", StringComparison.OrdinalIgnoreCase);

    private static bool IsMovementConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && (pg.ConstraintName ?? string.Empty).Contains("pk_cashier_shift_movements", StringComparison.OrdinalIgnoreCase);
}
