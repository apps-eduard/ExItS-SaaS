using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.Application.Expenses;

public sealed class ExpenseQueryService
{
    private readonly IExpenseRepository _expenses;
    private readonly IExpenseCategoryRepository _categories;

    public ExpenseQueryService(IExpenseRepository expenses, IExpenseCategoryRepository categories)
    {
        _expenses = expenses;
        _categories = categories;
    }

    public async Task<PosExpenseDto?> GetByIdAsync(
        Guid organizationId,
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var expense = await _expenses
            .GetByIdAsync(orgId, ExpenseId.From(expenseId), cancellationToken)
            .ConfigureAwait(false);
        if (expense is null)
        {
            return null;
        }

        return await MapEnrichedAsync(expense, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<PosExpenseDto>> ListAsync(
        Guid organizationId,
        ExpenseFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _expenses
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var categoryNames = await LoadCategoryNamesAsync(
                PosOrganizationId.From(organizationId),
                items.Select(e => e.CategoryId).Distinct(),
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosExpenseDto>(
            items.Select(e => Map(e, categoryNames.GetValueOrDefault(e.CategoryId.Value))).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PosExpenseDto Map(Expense expense, string? categoryName = null) =>
        new(
            expense.Id.Value,
            expense.OrganizationId.Value,
            expense.ExpenseNumber,
            expense.CategoryId.Value,
            categoryName,
            expense.Status.ToString(),
            ExpensePaymentMethods.ToCode(expense.PaymentMethod),
            expense.Amount,
            expense.Description,
            expense.Payee,
            expense.GCashReference,
            expense.ExpenseDate,
            expense.RecordedAtUtc,
            expense.RecordedBy,
            expense.VoidedAtUtc,
            expense.VoidedBy,
            expense.VoidReason,
            expense.UpdatedAtUtc);

    private async Task<PosExpenseDto> MapEnrichedAsync(Expense expense, CancellationToken cancellationToken)
    {
        var category = await _categories
            .GetByIdAsync(expense.OrganizationId, expense.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        return Map(expense, category?.Name);
    }

    private async Task<Dictionary<Guid, string>> LoadCategoryNamesAsync(
        PosOrganizationId organizationId,
        IEnumerable<ExpenseCategoryId> categoryIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();
        foreach (var categoryId in categoryIds)
        {
            var category = await _categories
                .GetByIdAsync(organizationId, categoryId, cancellationToken)
                .ConfigureAwait(false);
            if (category is not null)
            {
                result[categoryId.Value] = category.Name;
            }
        }

        return result;
    }
}

public sealed class RecordExpense
{
    private readonly IExpenseRepository _expenses;
    private readonly IExpenseCategoryRepository _categories;
    private readonly IClock _clock;

    public RecordExpense(
        IExpenseRepository expenses,
        IExpenseCategoryRepository categories,
        IClock clock)
    {
        _expenses = expenses;
        _categories = categories;
        _clock = clock;
    }

    public async Task<ApplicationResult<Expense>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        string paymentMethod,
        decimal amount,
        string description,
        DateOnly expenseDate,
        Guid actorId,
        string? payee = null,
        string? gcashReference = null,
        Guid? clientExpenseId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<Expense>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "A non-empty actor identifier is required.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            if (clientExpenseId is not null)
            {
                var existingById = await _expenses
                    .GetByIdAsync(orgId, ExpenseId.From(clientExpenseId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    return ApplicationResult<Expense>.Success(existingById);
                }
            }

            if (!ExpensePaymentMethods.TryParse(paymentMethod, out var method))
            {
                return ApplicationResult<Expense>.Failure(
                    DomainErrorCodes.InvalidExpensePaymentMethod,
                    $"Payment method must be one of: {string.Join(", ", ExpensePaymentMethods.Codes)}.");
            }

            var category = await _categories
                .GetByIdAsync(orgId, ExpenseCategoryId.From(categoryId), cancellationToken)
                .ConfigureAwait(false);
            if (category is null)
            {
                return ApplicationResult<Expense>.Failure(
                    ApplicationErrorCodes.ExpenseCategoryNotFound,
                    "Expense category was not found.");
            }

            if (category.Status != ExpenseCategoryStatus.Active)
            {
                return ApplicationResult<Expense>.Failure(
                    ApplicationErrorCodes.ExpenseCategoryNotAssignable,
                    "Expenses can only be recorded against an active category.");
            }

            var utcNow = _clock.UtcNow;
            var expense = await _expenses
                .RecordAsync(
                    orgId,
                    ExpenseNumbers.BusinessDateOf(utcNow),
                    expenseNumber => Expense.Record(
                        orgId,
                        expenseNumber,
                        category.Id,
                        method,
                        amount,
                        description,
                        expenseDate,
                        actorId,
                        utcNow,
                        payee,
                        gcashReference,
                        clientExpenseId is null ? null : ExpenseId.From(clientExpenseId.Value)),
                    cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<Expense>.Success(expense);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Expense>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Expense>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class VoidExpense
{
    private readonly IExpenseRepository _expenses;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public VoidExpense(
        IExpenseRepository expenses,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _expenses = expenses;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Expense>> ExecuteAsync(
        Guid organizationId,
        Guid expenseId,
        string reason,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<Expense>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "A non-empty actor identifier is required.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var current = await _expenses
            .GetByIdAsync(orgId, ExpenseId.From(expenseId), cancellationToken)
            .ConfigureAwait(false);
        if (current is null)
        {
            return ApplicationResult<Expense>.Failure(
                ApplicationErrorCodes.ExpenseNotFound,
                "Expense was not found.");
        }

        try
        {
            current.Void(reason, actorId, _clock.UtcNow);
            await _expenses.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Expense>.Success(current);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Expense>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Expense>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ExpenseSummaryService
{
    private readonly IExpenseRepository _expenses;
    private readonly IExpenseCategoryRepository _categories;

    public ExpenseSummaryService(IExpenseRepository expenses, IExpenseCategoryRepository categories)
    {
        _expenses = expenses;
        _categories = categories;
    }

    public async Task<PosExpenseSummaryDto> GetSummaryAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var items = await _expenses
            .ListForSummaryAsync(orgId, fromDate, toDate, cancellationToken)
            .ConfigureAwait(false);

        var recorded = items.Where(e => e.Status == ExpenseStatus.Recorded).ToList();
        var voided = items.Where(e => e.Status == ExpenseStatus.Voided).ToList();

        var gross = ExpenseMoney.RoundMoney(recorded.Sum(e => e.Amount));
        var voidedTotal = ExpenseMoney.RoundMoney(voided.Sum(e => e.Amount));
        var net = ExpenseMoney.RoundMoney(gross); // net excludes voided by using recorded only

        var categoryNames = new Dictionary<Guid, string>();
        foreach (var categoryId in recorded.Select(e => e.CategoryId).Distinct())
        {
            var category = await _categories
                .GetByIdAsync(orgId, categoryId, cancellationToken)
                .ConfigureAwait(false);
            if (category is not null)
            {
                categoryNames[categoryId.Value] = category.Name;
            }
        }

        var byCategory = recorded
            .GroupBy(e => e.CategoryId.Value)
            .Select(g => new ExpenseCategorySummaryDto(
                g.Key,
                categoryNames.GetValueOrDefault(g.Key),
                ExpenseMoney.RoundMoney(g.Sum(e => e.Amount)),
                g.Count()))
            .OrderByDescending(x => x.TotalAmount)
            .ThenBy(x => x.CategoryName)
            .ToList();

        var byPayment = recorded
            .GroupBy(e => ExpensePaymentMethods.ToCode(e.PaymentMethod))
            .Select(g => new ExpensePaymentSummaryDto(
                g.Key,
                ExpenseMoney.RoundMoney(g.Sum(e => e.Amount)),
                g.Count()))
            .OrderBy(x => x.PaymentMethod)
            .ToList();

        return new PosExpenseSummaryDto(
            fromDate,
            toDate,
            gross,
            voidedTotal,
            net,
            recorded.Count,
            voided.Count,
            byCategory,
            byPayment);
    }
}
