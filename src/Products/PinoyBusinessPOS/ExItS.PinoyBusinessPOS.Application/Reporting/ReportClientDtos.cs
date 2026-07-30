namespace ExItS.PinoyBusinessPOS.Application.Reporting;

// Export-ready tabular contracts (P8-WP06). File generation (CSV/PDF/Excel) is deferred; these DTOs
// are the stable on-screen and future-export shapes. Formulas are server-authoritative.

public sealed record ReportPeriodComparisonDto(
    DateOnly ComparisonFromDate,
    DateOnly ComparisonToDate,
    decimal? AbsoluteChange,
    decimal? PercentageChange,
    bool PercentageAvailable);

public sealed record ReportDailyAmountDto(DateOnly Date, decimal Amount, int Count);

public sealed record ReportPaymentBreakdownDto(string PaymentMethod, decimal Amount, int Count);

public sealed record PosDashboardDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal CompletedSalesTotal,
    int CompletedSaleCount,
    decimal CashSalesTotal,
    decimal ManualGCashSalesTotal,
    decimal UtangSalesTotal,
    decimal ActiveCustomerUtangOutstanding,
    decimal OverdueUtangAmount,
    decimal RecordedExpenseTotal,
    int LowStockProductCount,
    int VoidedSaleCount,
    int VoidedExpenseCount,
    IReadOnlyList<ReportDailyAmountDto> SalesByDay,
    IReadOnlyList<ReportDailyAmountDto> ExpensesByDay,
    IReadOnlyList<ReportPaymentBreakdownDto> PaymentMethodBreakdown,
    IReadOnlyList<ReportDailyAmountDto> SalesCountByDay,
    ReportPeriodComparisonDto? SalesTotalComparison,
    ReportPeriodComparisonDto? ExpenseTotalComparison);

public sealed record ReportProductSalesRowDto(
    Guid ProductId,
    string NameSnapshot,
    string? SkuSnapshot,
    string UnitOfMeasure,
    decimal Quantity,
    decimal SalesAmount,
    int LineCount,
    Guid? CategoryId,
    string? CategoryName);

public sealed record ReportCategorySalesRowDto(
    Guid? CategoryId,
    string CategoryName,
    decimal Quantity,
    decimal SalesAmount,
    int LineCount);

public sealed record PosSalesReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal CompletedSalesTotal,
    int CompletedSaleCount,
    decimal VoidedSalesTotal,
    int VoidedSaleCount,
    decimal UtangSalesTotal,
    int UtangSaleCount,
    IReadOnlyList<ReportPaymentBreakdownDto> ByPaymentMethod,
    IReadOnlyList<ReportProductSalesRowDto> ByProduct,
    IReadOnlyList<ReportCategorySalesRowDto> ByCategory,
    IReadOnlyList<ReportProductSalesRowDto> TopProductsByQuantity,
    IReadOnlyList<ReportProductSalesRowDto> TopProductsBySalesAmount,
    IReadOnlyList<ReportDailyAmountDto> ByDay);

public sealed record ReportCustomerBalanceRowDto(
    Guid CustomerId,
    string DisplayName,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int OverdueCreditCount,
    DateOnly? EarliestOverdueDate);

public sealed record PosUtangReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal ActiveCustomerOutstanding,
    decimal OverdueAmount,
    int CustomersWithBalances,
    int CustomersWithOverdue,
    decimal CreditsRecordedInPeriod,
    int CreditsRecordedCount,
    decimal RepaymentsRecordedInPeriod,
    int RepaymentsRecordedCount,
    decimal ProductBasedUtangSalesInPeriod,
    int ProductBasedUtangSaleCount,
    IReadOnlyList<ReportCustomerBalanceRowDto> CustomersWithBalancesList,
    IReadOnlyList<ReportCustomerBalanceRowDto> CustomersWithOverdueList);

public sealed record ReportInventoryStatusRowDto(
    Guid CatalogProductId,
    string ProductName,
    string? Sku,
    bool IsTracked,
    decimal OnHandQuantity,
    decimal? ReorderLevel,
    bool IsLowStock,
    bool IsOutOfStock,
    DateTimeOffset? LatestMovementAtUtc);

public sealed record ReportMovementTypeTotalDto(string MovementType, decimal QuantityTotal, int Count);

public sealed record PosInventoryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TrackedProductCount,
    int LowStockProductCount,
    int OutOfStockProductCount,
    DateTimeOffset? LatestMovementAtUtc,
    IReadOnlyList<ReportMovementTypeTotalDto> MovementsByType,
    IReadOnlyList<ReportInventoryStatusRowDto> TrackedProducts,
    IReadOnlyList<ReportInventoryStatusRowDto> LowStockProducts,
    IReadOnlyList<ReportInventoryStatusRowDto> OutOfStockProducts);

public sealed record ReportExpenseDetailRowDto(
    Guid ExpenseId,
    string ExpenseNumber,
    Guid CategoryId,
    string? CategoryName,
    string Status,
    string PaymentMethod,
    decimal Amount,
    string Description,
    string? Payee,
    DateOnly ExpenseDate,
    DateTimeOffset RecordedAtUtc);

public sealed record PosExpensesReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal ActiveExpenseTotal,
    decimal VoidedExpenseTotal,
    int ActiveExpenseCount,
    int VoidedExpenseCount,
    IReadOnlyList<ReportPaymentBreakdownDto> ByPaymentMethod,
    IReadOnlyList<ExpenseCategoryReportRowDto> ByCategory,
    IReadOnlyList<ReportDailyAmountDto> ByDay,
    IReadOnlyList<ReportExpenseDetailRowDto> Details);

public sealed record ExpenseCategoryReportRowDto(
    Guid CategoryId,
    string? CategoryName,
    decimal TotalAmount,
    int Count);
