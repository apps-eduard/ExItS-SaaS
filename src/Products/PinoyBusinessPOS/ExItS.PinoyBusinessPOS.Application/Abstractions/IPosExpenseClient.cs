using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS expenses API client. Online-only for P8-WP05 — offline calls fail fast and no
/// expense mutation is ever queued locally.
/// </summary>
public interface IPosExpenseClient
{
    Task<ApiResult<PosExpenseCategoryPagedResult>> ListCategoriesAsync(
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosExpenseCategoryDto>> GetCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<ApiResult<PosExpenseCategoryDto>> CreateCategoryAsync(
        CreatePosExpenseCategoryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosExpenseCategoryDto>> UpdateCategoryAsync(
        Guid categoryId,
        UpdatePosExpenseCategoryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosExpenseCategoryDto>> DeactivateCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<ApiResult<PosExpenseCategoryDto>> ReactivateCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<ApiResult<PosExpensePagedResult>> ListExpensesAsync(
        string? status = null,
        string? paymentMethod = null,
        Guid? categoryId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? expenseNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosExpenseDto>> GetExpenseAsync(Guid expenseId, CancellationToken ct = default);

    Task<ApiResult<PosExpenseDto>> RecordExpenseAsync(RecordExpenseRequest request, CancellationToken ct = default);

    Task<ApiResult<PosExpenseDto>> VoidExpenseAsync(
        Guid expenseId,
        VoidExpenseRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosExpenseSummaryDto>> GetSummaryAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);
}
