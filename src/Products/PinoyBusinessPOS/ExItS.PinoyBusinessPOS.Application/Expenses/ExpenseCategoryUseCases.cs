using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.Application.Expenses;

public sealed class ExpenseCategoryQueryService
{
    private readonly IExpenseCategoryRepository _categories;

    public ExpenseCategoryQueryService(IExpenseCategoryRepository categories) => _categories = categories;

    public async Task<PosExpenseCategoryDto?> GetByIdAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ExpenseCategoryId.From(categoryId),
                cancellationToken)
            .ConfigureAwait(false);
        return category is null ? null : Map(category);
    }

    public async Task<PagedResult<PosExpenseCategoryDto>> ListAsync(
        Guid organizationId,
        ExpenseCategoryStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _categories
            .ListAsync(PosOrganizationId.From(organizationId), status, search, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosExpenseCategoryDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PosExpenseCategoryDto Map(ExpenseCategory category) =>
        new(
            category.Id.Value,
            category.OrganizationId.Value,
            category.Name,
            category.Status.ToString(),
            category.CreatedAtUtc,
            category.UpdatedAtUtc);
}

public sealed class CreateExpenseCategory
{
    private readonly IExpenseCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateExpenseCategory(
        IExpenseCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpenseCategory>> ExecuteAsync(
        Guid organizationId,
        string name,
        Guid? clientCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            if (clientCategoryId is not null)
            {
                var existingById = await _categories
                    .GetByIdAsync(orgId, ExpenseCategoryId.From(clientCategoryId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    return ApplicationResult<ExpenseCategory>.Success(existingById);
                }
            }

            var category = clientCategoryId is null
                ? ExpenseCategory.Create(orgId, name, _clock.UtcNow)
                : ExpenseCategory.Create(
                    orgId,
                    name,
                    _clock.UtcNow,
                    ExpenseCategoryId.From(clientCategoryId.Value));

            var duplicate = await _categories
                .FindActiveByNormalizedNameAsync(orgId, category.NormalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null)
            {
                return ApplicationResult<ExpenseCategory>.Failure(
                    ApplicationErrorCodes.ExpenseCategoryNameConflict,
                    "An active expense category with this name already exists in this organization.");
            }

            await _categories.AddAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpenseCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateExpenseCategory
{
    private readonly IExpenseCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateExpenseCategory(
        IExpenseCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpenseCategory>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        string name,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var category = await _categories
            .GetByIdAsync(orgId, ExpenseCategoryId.From(categoryId), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<ExpenseCategory>.Failure(
                ApplicationErrorCodes.ExpenseCategoryNotFound,
                "Expense category was not found.");
        }

        if (ExpenseConcurrency.IsStale(expectedUpdatedAtUtc, category.UpdatedAtUtc))
        {
            return ApplicationResult<ExpenseCategory>.Failure(
                ApplicationErrorCodes.ExpenseConcurrencyConflict,
                "The expense category was updated concurrently. Reload the latest version and try again.");
        }

        try
        {
            var normalized = ExpenseCategory.NormalizeForLookup(name);
            if (!string.Equals(normalized, category.NormalizedName, StringComparison.Ordinal))
            {
                var duplicate = await _categories
                    .FindActiveByNormalizedNameAsync(orgId, normalized, cancellationToken)
                    .ConfigureAwait(false);
                if (duplicate is not null && duplicate.Id != category.Id)
                {
                    return ApplicationResult<ExpenseCategory>.Failure(
                        ApplicationErrorCodes.ExpenseCategoryNameConflict,
                        "An active expense category with this name already exists in this organization.");
                }
            }

            category.Rename(name, _clock.UtcNow);
            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpenseCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivateExpenseCategory
{
    private readonly IExpenseCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivateExpenseCategory(
        IExpenseCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpenseCategory>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ExpenseCategoryId.From(categoryId),
                cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<ExpenseCategory>.Failure(
                ApplicationErrorCodes.ExpenseCategoryNotFound,
                "Expense category was not found.");
        }

        try
        {
            category.Deactivate(_clock.UtcNow);
            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpenseCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivateExpenseCategory
{
    private readonly IExpenseCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivateExpenseCategory(
        IExpenseCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpenseCategory>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var category = await _categories
            .GetByIdAsync(orgId, ExpenseCategoryId.From(categoryId), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<ExpenseCategory>.Failure(
                ApplicationErrorCodes.ExpenseCategoryNotFound,
                "Expense category was not found.");
        }

        try
        {
            var duplicate = await _categories
                .FindActiveByNormalizedNameAsync(orgId, category.NormalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null && duplicate.Id != category.Id)
            {
                return ApplicationResult<ExpenseCategory>.Failure(
                    ApplicationErrorCodes.ExpenseCategoryNameConflict,
                    "An active expense category with this name already exists in this organization.");
            }

            category.Reactivate(_clock.UtcNow);
            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpenseCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ExpenseCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class ExpenseConcurrency
{
    public static bool IsStale(DateTimeOffset? expectedUpdatedAtUtc, DateTimeOffset actualUpdatedAtUtc)
    {
        if (expectedUpdatedAtUtc is null)
        {
            return false;
        }

        return expectedUpdatedAtUtc.Value.ToUniversalTime().UtcTicks
               != actualUpdatedAtUtc.ToUniversalTime().UtcTicks;
    }
}
