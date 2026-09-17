using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Expenses;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository : IExpenseRepository
{
    /// <summary>
    /// Transaction-scoped advisory lock over the (organization, business date) expense counter.
    /// Salted distinctly from sale sequences so the two counters do not share a lock key.
    /// </summary>
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";
    private const ulong ExpenseLockSalt = 0x4558504e53450001UL; // "EXPENSE\x01"

    private readonly PosDbContext _db;

    public ExpenseRepository(PosDbContext db) => _db = db;

    public async Task<Expense?> GetByIdAsync(
        PosOrganizationId organizationId,
        ExpenseId expenseId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Expenses.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == expenseId.Value && e.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ExpenseEntityMapper.ToDomain(record);
    }

    public async Task<Expense?> FindByExpenseNumberAsync(
        PosOrganizationId organizationId,
        string expenseNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = ExpenseNumbers.Normalize(expenseNumber);
        var record = await _db.Expenses.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.OrganizationId == organizationId.Value && e.ExpenseNumber == normalized,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ExpenseEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<Expense> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ExpenseFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_db.Expenses.AsNoTracking().Where(e => e.OrganizationId == organizationId.Value), filter);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.RecordedAtUtc)
            .ThenByDescending(e => e.ExpenseNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(ExpenseEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<Expense>> ListForSummaryAsync(
        PosOrganizationId organizationId,
        ExpenseFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_db.Expenses.AsNoTracking().Where(e => e.OrganizationId == organizationId.Value), filter);
        var records = await query
            .OrderBy(e => e.ExpenseDate)
            .ThenBy(e => e.ExpenseNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ExpenseEntityMapper.ToDomain).ToList();
    }

    public async Task<Expense> RecordAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, Expense> createExpense,
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteRecordAsync(organizationId, businessDateUtc, createExpense, cancellationToken)
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
                var expense = await CompleteRecordAsync(
                        organizationId,
                        businessDateUtc,
                        createExpense,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return expense;
            }
            catch (DbUpdateException ex) when (IsExpenseNumberConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.ExpenseNumberConflict,
                    "An expense number was allocated concurrently. Retry recording the expense.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<Expense> CompleteRecordAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, Expense> createExpense,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, cancellationToken)
                .ConfigureAwait(false);
            var expense = createExpense(ExpenseNumbers.Format(businessDateUtc, sequence));

            _db.Expenses.Add(ExpenseEntityMapper.ToRecord(expense));
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return expense;
        }
        catch (DbUpdateException ex) when (IsExpenseNumberConflict(ex))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ExpenseNumberConflict,
                "An expense number was allocated concurrently. Retry recording the expense.");
        }
    }

    public async Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        var record = await _db.Expenses
            .FirstOrDefaultAsync(
                e => e.Id == expense.Id.Value && e.OrganizationId == expense.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(ApplicationErrorCodes.ExpenseNotFound, "Expense was not found.");
        }

        ExpenseEntityMapper.ApplyToRecord(expense, record);
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

        var sequence = await _db.ExpenseNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);

        if (sequence is null)
        {
            _db.ExpenseNumberSequences.Add(new ExpenseNumberSequenceRecord
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
            var hash = ExpenseLockSalt;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)hash;
        }
    }

    private static IQueryable<ExpenseRecord> ApplyFilter(IQueryable<ExpenseRecord> query, ExpenseFilter filter)
    {
        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(e => e.Status == statusName);
        }

        if (filter.PaymentMethod is not null)
        {
            var methodCode = ExpensePaymentMethods.ToCode(filter.PaymentMethod.Value);
            query = query.Where(e => e.PaymentMethod == methodCode);
        }

        if (filter.CategoryId is not null)
        {
            var categoryId = filter.CategoryId.Value;
            query = query.Where(e => e.CategoryId == categoryId);
        }

        if (filter.FromDate is not null)
        {
            var from = filter.FromDate.Value;
            query = query.Where(e => e.ExpenseDate >= from);
        }

        if (filter.ToDate is not null)
        {
            var to = filter.ToDate.Value;
            query = query.Where(e => e.ExpenseDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.ExpenseNumber))
        {
            var term = filter.ExpenseNumber.Trim().ToUpperInvariant();
            query = query.Where(e => e.ExpenseNumber.Contains(term));
        }

        if (filter.BranchScope is not null)
        {
            query = ApplyBranchScope(query, filter.BranchScope);
        }

        return query;
    }

    private static IQueryable<ExpenseRecord> ApplyBranchScope(
        IQueryable<ExpenseRecord> query,
        ExpenseBranchScopeCriteria scope)
    {
        switch (scope.Kind)
        {
            case ExpenseBranchScopeKind.SingleBranch:
            {
                var branchId = scope.BranchId;
                if (branchId is null)
                {
                    return query.Where(_ => false);
                }

                return query.Where(e => e.BranchId == branchId);
            }
            case ExpenseBranchScopeKind.AllAuthorizedBranches:
            {
                var ids = scope.AuthorizedBranchIds?.ToList() ?? [];
                if (ids.Count == 0)
                {
                    return query.Where(_ => false);
                }

                return query.Where(e => e.BranchId != null && ids.Contains(e.BranchId.Value));
            }
            case ExpenseBranchScopeKind.OrganizationWide:
                return query.Where(e => e.BranchId == null);
            case ExpenseBranchScopeKind.AllExpenses:
            {
                var ids = scope.AuthorizedBranchIds?.ToList() ?? [];
                if (ids.Count == 0)
                {
                    return query.Where(e => e.BranchId == null);
                }

                return query.Where(e => e.BranchId == null || ids.Contains(e.BranchId.Value));
            }
            default:
                return query.Where(_ => false);
        }
    }

    private static bool IsExpenseNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && (pg.ConstraintName ?? string.Empty).Contains("ux_expenses_org_expense_number", StringComparison.OrdinalIgnoreCase);
}
