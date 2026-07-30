namespace ExItS.PinoyBusinessPOS.Application.Expenses;

public sealed record PosExpenseCategoryDto(
    Guid CategoryId,
    Guid OrganizationId,
    string Name,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreatePosExpenseCategoryRequest(string Name, Guid? CategoryId = null);

public sealed record UpdatePosExpenseCategoryRequest(
    string Name,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record PosExpenseCategoryPagedResult(
    List<PosExpenseCategoryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosExpenseDto(
    Guid ExpenseId,
    Guid OrganizationId,
    string ExpenseNumber,
    Guid CategoryId,
    string? CategoryName,
    string Status,
    string PaymentMethod,
    decimal Amount,
    string Description,
    string? Payee,
    string? GCashReference,
    DateOnly ExpenseDate,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy,
    DateTimeOffset? VoidedAtUtc,
    Guid? VoidedBy,
    string? VoidReason,
    DateTimeOffset UpdatedAtUtc);

public sealed record RecordExpenseRequest(
    Guid CategoryId,
    string PaymentMethod,
    decimal Amount,
    string Description,
    DateOnly ExpenseDate,
    string? Payee = null,
    string? GCashReference = null,
    Guid? ExpenseId = null);

public sealed record VoidExpenseRequest(string Reason);

public sealed record PosExpensePagedResult(
    List<PosExpenseDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ExpenseCategorySummaryDto(
    Guid CategoryId,
    string? CategoryName,
    decimal TotalAmount,
    int Count);

public sealed record ExpensePaymentSummaryDto(
    string PaymentMethod,
    decimal TotalAmount,
    int Count);

public sealed record PosExpenseSummaryDto(
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal GrossTotal,
    decimal VoidedTotal,
    decimal NetTotal,
    int RecordedCount,
    int VoidedCount,
    IReadOnlyList<ExpenseCategorySummaryDto> ByCategory,
    IReadOnlyList<ExpensePaymentSummaryDto> ByPaymentMethod);
