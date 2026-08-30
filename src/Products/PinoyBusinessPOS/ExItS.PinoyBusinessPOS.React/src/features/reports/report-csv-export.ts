import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  formatReportPaymentMethod,
  getCashVarianceReport,
  getExpenseSummaryReport,
  getExpensesReport,
  getInventoryMovementsReport,
  getInventoryReport,
  getInventoryStatusReport,
  getOperationalOverview,
  getProductUtangSummaryReport,
  getProfitabilityReport,
  getProductProfitabilityReport,
  getPurchaseOutstandingReport,
  getPurchasingSummaryReport,
  getReturnsReport,
  getSalesByPaymentReport,
  getSalesByProductReport,
  getSalesReport,
  getSalesSummaryReport,
  getShiftSummaryReport,
  getStockCountVarianceReport,
  getSupplierPayablesReport,
  getSupplierPurchasingReport,
  getUtangReport,
  type PosProductProfitabilityRowDto,
} from "@/api/pos/pos-reporting-client";
import type { ClassicReportKind, OperationalReportKind } from "@/features/reports/report-access";
import type { ReportDateRangeValue } from "@/features/reports/report-date-range";
import {
  buildCsvWithMetadata,
  buildReportCsvFilename,
  downloadCsvFile,
  type CsvCell,
  type CsvTable,
} from "@/lib/csv";

export type ReportExportScopeInfo = {
  organizationName?: string | null;
  scopeLabel: string;
  fromDate?: string | null;
  toDate?: string | null;
};

export type ReportExportResult = {
  filename: string;
  csvText: string;
  rowCount: number;
};

function metricsTable(rows: Array<{ metric: string; value: CsvCell }>): CsvTable {
  return {
    headers: ["Metric", "Value"],
    rows: rows.map((row) => [row.metric, row.value]),
  };
}

function isInternalIdKey(key: string): boolean {
  return key === "id" || /Id$/.test(key) || key.endsWith("ID");
}

function humanizeHeader(key: string): string {
  return key
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .replace(/\b\w/g, (ch) => ch.toUpperCase())
    .trim();
}

function recordRowsTable(
  rows: Array<Record<string, unknown>>,
  preferredKeys: string[],
): CsvTable {
  if (rows.length === 0) {
    return { headers: preferredKeys.map(humanizeHeader), rows: [] };
  }
  const keys = new Set<string>();
  for (const row of rows) {
    for (const key of Object.keys(row)) {
      if (!isInternalIdKey(key)) {
        keys.add(key);
      }
    }
  }
  const orderedKeys = [
    ...preferredKeys.filter((key) => keys.has(key)),
    ...[...keys].filter((key) => !preferredKeys.includes(key)).sort(),
  ];
  return {
    headers: orderedKeys.map(humanizeHeader),
    rows: rows.map((row) =>
      orderedKeys.map((key) => {
        const value = row[key];
        if (value === null || value === undefined) {
          return "";
        }
        if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
          return value;
        }
        return String(value);
      }),
    ),
  };
}

export function resolveReportExportScopeLabel(args: {
  scopeMode: "branch" | "all" | "organization_only";
  selection: { mode: "current" } | { mode: "all" } | { mode: "branch"; branchId: string };
  currentBranchName?: string | null;
  selectedBranchName?: string | null;
}): string {
  if (args.scopeMode === "organization_only" || args.selection.mode === "all") {
    return "all-branches";
  }
  if (args.selection.mode === "branch") {
    return args.selectedBranchName?.trim() || "branch";
  }
  return args.currentBranchName?.trim() || "current-branch";
}

function finalizeExport(
  reportName: string,
  scope: ReportExportScopeInfo,
  table: CsvTable,
): ReportExportResult {
  const metadata: Array<readonly [string, string]> = [["Report", reportName]];
  if (scope.organizationName) {
    metadata.push(["Organization", scope.organizationName]);
  }
  metadata.push(["Scope", scope.scopeLabel]);
  if (scope.fromDate) {
    metadata.push(["From", scope.fromDate]);
  }
  if (scope.toDate) {
    metadata.push(["To", scope.toDate]);
  }
  metadata.push(["ExportedAtUtc", new Date().toISOString()]);

  return {
    filename: buildReportCsvFilename({
      reportName,
      scopeLabel: scope.scopeLabel,
      fromDate: scope.fromDate,
      toDate: scope.toDate,
    }),
    csvText: buildCsvWithMetadata(metadata, table),
    rowCount: table.rows.length,
  };
}

export function buildProductProfitabilityCsvTable(
  rows: PosProductProfitabilityRowDto[],
): CsvTable {
  return {
    headers: [
      "Product",
      "SKU",
      "Unit",
      "Qty Sold",
      "Qty Returned",
      "Net Qty",
      "Sales Before Discounts",
      "Commercial Discounts",
      "Net Sales",
      "Refunds",
      "Known COGS",
      "COGS Status",
      "Total COGS",
      "Gross Profit",
      "Margin %",
      "Cost Completeness %",
    ],
    rows: rows.map((row) => [
      row.productName,
      row.sku ?? "",
      row.unitOfMeasure,
      row.quantitySold,
      row.quantityReturned,
      row.netQuantity,
      row.salesBeforeDiscounts,
      row.commercialDiscounts,
      row.netSales,
      row.refundAmount,
      row.knownCogs,
      row.cogsStatus,
      row.totalCogs ?? "",
      row.grossProfit ?? "",
      row.grossMarginPercent ?? "",
      row.costCompletenessPercent,
    ]),
  };
}

export async function buildOperationalReportExport(args: {
  kind: OperationalReportKind;
  workspace: PosWorkspaceScope;
  range: ReportDateRangeValue;
  reportBranchId?: string | null;
  scope: ReportExportScopeInfo;
  rankBy?: string | null;
  signal?: AbortSignal;
  productProfitabilityRows?: PosProductProfitabilityRowDto[] | null;
}): Promise<ReportExportResult> {
  const { kind, workspace, range, reportBranchId, scope, signal } = args;

  switch (kind) {
    case "product-profitability": {
      const rows =
        args.productProfitabilityRows ??
        (
          await getProductProfitabilityReport(
            workspace,
            range,
            signal,
            reportBranchId,
            args.rankBy,
          )
        ).rows;
      return finalizeExport("product-profitability", scope, buildProductProfitabilityCsvTable(rows));
    }
    case "sales-by-product": {
      const data = await getSalesByProductReport(workspace, range, signal, reportBranchId);
      return finalizeExport("sales-by-product", scope, {
        headers: [
          "Product",
          "Unit",
          "Selling Mode",
          "Qty Sold",
          "Qty Returned",
          "Net Qty",
          "Sales Before Discounts",
          "Commercial Discounts",
          "Gross Sales",
          "Refunds",
          "Net Sales",
        ],
        rows: data.rows.map((row) => [
          row.productName,
          row.unitOfMeasure,
          row.sellingMode,
          row.quantitySold,
          row.quantityReturned,
          row.netQuantity,
          row.preDiscountGrossSaleAmount,
          row.commercialDiscountAmount,
          row.grossSaleAmount,
          row.refundAmount,
          row.netAmount,
        ]),
      });
    }
    case "sales-by-payment": {
      const data = await getSalesByPaymentReport(workspace, range, signal, reportBranchId);
      return finalizeExport("sales-by-payment", scope, {
        headers: [
          "Payment Method",
          "Sales Before Discounts",
          "Commercial Discounts",
          "Gross Completed",
          "Voided",
          "Refunded",
          "Net",
        ],
        rows: data.rows.map((row) => [
          formatReportPaymentMethod(row.paymentMethod),
          row.preDiscountGross,
          row.commercialDiscountTotal,
          row.grossCompleted,
          row.voided,
          row.refunded,
          row.net,
        ]),
      });
    }
    case "supplier-purchasing": {
      const data = await getSupplierPurchasingReport(workspace, range, signal);
      return finalizeExport(
        "supplier-purchasing",
        scope,
        recordRowsTable(data.rows ?? [], [
          "supplierName",
          "orderedQuantity",
          "receivedQuantity",
          "outstandingQuantity",
          "orderCount",
        ]),
      );
    }
    case "supplier-payables": {
      const rows = await getSupplierPayablesReport(
        workspace,
        { outstandingOnly: true },
        signal,
      );
      return finalizeExport("supplier-payables", scope, {
        headers: [
          "Supplier",
          "Source Type",
          "Original Amount",
          "Paid At Receipt",
          "Paid Amount",
          "Balance",
          "Status",
          "Due Date",
          "Overdue",
          "Created At Utc",
        ],
        rows: rows.map((row) => [
          row.supplierName ?? "",
          row.sourceType,
          row.originalAmount,
          row.paidAtReceiptAmount,
          row.paidAmount,
          row.balance,
          row.status,
          row.dueDate ?? "",
          row.isOverdue,
          row.createdAtUtc,
        ]),
      });
    }
    case "inventory-status": {
      const data = await getInventoryStatusReport(workspace, signal);
      const scoped = { ...scope, fromDate: data.asOfDate, toDate: data.asOfDate };
      if (data.rows && data.rows.length > 0) {
        return finalizeExport(
          "inventory-status",
          scoped,
          recordRowsTable(data.rows, [
            "productName",
            "sku",
            "onHandQuantity",
            "reorderLevel",
            "isLowStock",
            "isOutOfStock",
          ]),
        );
      }
      return finalizeExport(
        "inventory-status",
        scoped,
        metricsTable([
          { metric: "Tracked products", value: data.trackedCount },
          { metric: "Low stock", value: data.lowStockCount },
          { metric: "Out of stock", value: data.outOfStockCount },
        ]),
      );
    }
    case "inventory-movements": {
      const data = await getInventoryMovementsReport(workspace, range, signal, reportBranchId);
      return finalizeExport("inventory-movements", scope, {
        headers: ["Movement Type", "Quantity Total", "Count"],
        rows: data.byType.map((row) => [row.movementType, row.quantityTotal, row.count]),
      });
    }
    case "expenses-summary": {
      const data = await getExpenseSummaryReport(workspace, range, signal);
      if (data.byCategory && data.byCategory.length > 0) {
        return finalizeExport(
          "expenses-summary",
          scope,
          recordRowsTable(data.byCategory, ["categoryName", "amount", "count"]),
        );
      }
      return finalizeExport(
        "expenses-summary",
        scope,
        metricsTable([
          { metric: "Recorded total", value: data.recordedTotal },
          { metric: "Voided total", value: data.voidedTotal },
          { metric: "Recorded count", value: data.recordedCount },
          { metric: "Voided count", value: data.voidedCount },
        ]),
      );
    }
    case "utang-by-product": {
      const data = await getProductUtangSummaryReport(workspace, range, signal);
      if (data.byProduct && data.byProduct.length > 0) {
        return finalizeExport(
          "utang-by-product",
          scope,
          recordRowsTable(data.byProduct, [
            "productName",
            "utangSales",
            "outstanding",
            "overdue",
          ]),
        );
      }
      return finalizeExport(
        "utang-by-product",
        scope,
        metricsTable([
          { metric: "Utang sales", value: data.utangSalesTotal },
          { metric: "Utang sale count", value: data.utangSaleCount },
          { metric: "Outstanding", value: data.outstandingTotal },
          { metric: "Overdue", value: data.overdueTotal },
        ]),
      );
    }
    case "purchasing-summary": {
      const data = await getPurchasingSummaryReport(workspace, range, signal);
      return finalizeExport(
        "purchasing-summary",
        scope,
        metricsTable([
          { metric: "Orders", value: data.orderCount },
          { metric: "Ordered quantity", value: data.orderedQuantity },
          { metric: "Received quantity", value: data.receivedQuantity },
          { metric: "Outstanding quantity", value: data.outstandingQuantity },
        ]),
      );
    }
    case "purchase-outstanding": {
      const data = await getPurchaseOutstandingReport(workspace, signal);
      const scoped = { ...scope, fromDate: data.asOfDate, toDate: data.asOfDate };
      if (data.rows && data.rows.length > 0) {
        return finalizeExport(
          "purchase-outstanding",
          scoped,
          recordRowsTable(data.rows, [
            "supplierName",
            "orderNumber",
            "outstandingQuantity",
            "status",
          ]),
        );
      }
      return finalizeExport(
        "purchase-outstanding",
        scoped,
        metricsTable([
          { metric: "Outstanding orders", value: data.outstandingOrderCount },
          { metric: "Outstanding quantity", value: data.outstandingQuantity },
        ]),
      );
    }
    case "profitability": {
      const data = await getProfitabilityReport(workspace, range, signal, reportBranchId);
      return finalizeExport(
        "profitability",
        scope,
        metricsTable([
          { metric: "Net sales", value: data.netSales },
          { metric: "Commercial discounts", value: data.commercialDiscountTotal },
          { metric: "COGS status", value: data.cogsStatus },
          { metric: "Known COGS", value: data.knownCogs },
          { metric: "Total COGS", value: data.totalCogs ?? "" },
          { metric: "Gross profit", value: data.grossProfit ?? "" },
          { metric: "Gross margin %", value: data.grossMarginPercent ?? "" },
          { metric: "Waste/loss known cost", value: data.wasteLossKnownCost },
          { metric: "Waste/loss cost status", value: data.wasteLossCostStatus },
          { metric: "Stock use known cost", value: data.stockUseKnownCost },
          { metric: "Stock use cost status", value: data.stockUseCostStatus },
          { metric: "Cost completeness %", value: data.costCompletenessPercent },
          { metric: "Completed sales", value: data.completedSaleCount },
        ]),
      );
    }
    case "sales-summary": {
      const data = await getSalesSummaryReport(workspace, range, signal, reportBranchId);
      return finalizeExport(
        "sales-summary",
        scope,
        metricsTable([
          { metric: "Sales before discounts", value: data.preDiscountGrossSales },
          { metric: "Commercial discounts", value: data.commercialDiscountTotal },
          { metric: "Net subtotal", value: data.netSubtotal },
          { metric: "Tax", value: data.taxAmount },
          { metric: "Completed gross sales", value: data.completedGrossSales },
          { metric: "Voided sales", value: data.voidedSales },
          { metric: "Returns / refunds", value: data.completedReturnsRefunds },
          { metric: "Net sales", value: data.netSales },
          { metric: "Transactions", value: data.completedTransactionCount },
        ]),
      );
    }
    case "overview": {
      const data = await getOperationalOverview(workspace, range, signal, reportBranchId);
      return finalizeExport(
        "sales-overview",
        scope,
        metricsTable([
          { metric: "Sales before discounts", value: data.preDiscountGrossSales },
          { metric: "Commercial discounts", value: data.commercialDiscountTotal },
          { metric: "Net subtotal", value: data.netSubtotal },
          { metric: "Tax", value: data.taxAmount },
          { metric: "Completed gross sales", value: data.completedGrossSales },
          { metric: "Voided sales", value: data.voidedSales },
          { metric: "Refunds", value: data.refunds },
          { metric: "Net sales", value: data.netSales },
          { metric: "Transactions", value: data.completedTransactionCount },
          { metric: "Average transaction", value: data.averageTransactionValue },
        ]),
      );
    }
    case "returns": {
      const data = await getReturnsReport(workspace, range, signal, reportBranchId);
      return finalizeExport(
        "returns",
        scope,
        metricsTable([
          { metric: "Return count", value: data.returnCount },
          { metric: "Returned quantity", value: data.returnedQuantity },
          { metric: "Refund amount", value: data.refundAmount },
        ]),
      );
    }
    case "shifts": {
      const data = await getShiftSummaryReport(workspace, range, signal);
      return finalizeExport(
        "shifts",
        scope,
        metricsTable([
          { metric: "Shift count", value: data.shiftCount },
          { metric: "Total cash variance", value: data.totalCashVariance },
        ]),
      );
    }
    case "cash-variance": {
      const data = await getCashVarianceReport(workspace, range, signal);
      return finalizeExport(
        "cash-variance",
        scope,
        metricsTable([
          { metric: "Closed shifts", value: data.closedShiftCount },
          { metric: "Absolute variance", value: data.totalAbsoluteVariance },
          { metric: "Signed variance", value: data.totalSignedVariance },
        ]),
      );
    }
    case "stock-count-variance": {
      const data = await getStockCountVarianceReport(workspace, range, signal);
      return finalizeExport(
        "stock-count-variance",
        scope,
        metricsTable([
          { metric: "Completed counts", value: data.completedCount },
          { metric: "Variance lines", value: data.varianceLineCount },
        ]),
      );
    }
    default: {
      const _exhaustive: never = kind;
      void _exhaustive;
      throw new Error(`Unsupported operational export kind: ${String(kind)}`);
    }
  }
}

export async function buildClassicReportExport(args: {
  kind: ClassicReportKind;
  workspace: PosWorkspaceScope;
  range: ReportDateRangeValue;
  reportBranchId?: string | null;
  scope: ReportExportScopeInfo;
  signal?: AbortSignal;
}): Promise<ReportExportResult> {
  const { kind, workspace, range, reportBranchId, scope, signal } = args;

  if (kind === "sales") {
    const data = await getSalesReport(workspace, range, signal, reportBranchId);
    if (data.byProduct && data.byProduct.length > 0) {
      return finalizeExport(
        "classic-sales",
        scope,
        recordRowsTable(data.byProduct, [
          "nameSnapshot",
          "skuSnapshot",
          "quantity",
          "salesAmount",
          "commercialDiscountAmount",
          "preDiscountGrossSaleAmount",
        ]),
      );
    }
    return finalizeExport("classic-sales", scope, {
      headers: ["Payment Method", "Amount", "Count"],
      rows: [
        ["(summary) Sales before discounts", data.preDiscountGrossSales, ""],
        ["(summary) Commercial discounts", data.commercialDiscountTotal, ""],
        ["(summary) Net subtotal", data.netSubtotal, ""],
        ["(summary) Tax", data.taxAmount, ""],
        ["(summary) Completed sales", data.completedSalesTotal, data.completedSaleCount],
        ["(summary) Voided sales", data.voidedSalesTotal, data.voidedSaleCount],
        ["(summary) Utang sales", data.utangSalesTotal, data.utangSaleCount],
        ...data.byPaymentMethod.map((row) => [
          formatReportPaymentMethod(row.paymentMethod),
          row.amount,
          row.count,
        ]),
      ],
    });
  }

  if (kind === "utang") {
    const data = await getUtangReport(workspace, range, signal);
    if (data.customersWithBalancesList && data.customersWithBalancesList.length > 0) {
      return finalizeExport(
        "classic-utang",
        scope,
        recordRowsTable(data.customersWithBalancesList, [
          "displayName",
          "outstandingAmount",
          "overdueAmount",
        ]),
      );
    }
    return finalizeExport(
      "classic-utang",
      scope,
      metricsTable([
        { metric: "Outstanding", value: data.activeCustomerOutstanding },
        { metric: "Overdue", value: data.overdueAmount },
        { metric: "Utang sales (period)", value: data.productBasedUtangSalesInPeriod },
        { metric: "Customers with balances", value: data.customersWithBalances },
        { metric: "Customers with overdue", value: data.customersWithOverdue },
      ]),
    );
  }

  if (kind === "inventory") {
    const data = await getInventoryReport(workspace, range, signal);
    if (data.trackedProducts && data.trackedProducts.length > 0) {
      return finalizeExport(
        "classic-inventory",
        scope,
        recordRowsTable(data.trackedProducts, [
          "productName",
          "sku",
          "onHandQuantity",
          "reorderLevel",
          "isLowStock",
          "isOutOfStock",
        ]),
      );
    }
    return finalizeExport(
      "classic-inventory",
      scope,
      metricsTable([
        { metric: "Tracked products", value: data.trackedProductCount },
        { metric: "Low stock", value: data.lowStockProductCount },
        { metric: "Out of stock", value: data.outOfStockProductCount },
      ]),
    );
  }

  const data = await getExpensesReport(workspace, range, signal);
  if (data.details && data.details.length > 0) {
    return finalizeExport(
      "classic-expenses",
      scope,
      recordRowsTable(data.details, [
        "expenseNumber",
        "categoryName",
        "amount",
        "expenseDate",
        "paymentMethod",
        "status",
      ]),
    );
  }
  if (data.byCategory && data.byCategory.length > 0) {
    return finalizeExport(
      "classic-expenses",
      scope,
      recordRowsTable(data.byCategory, ["categoryName", "amount", "count"]),
    );
  }
  return finalizeExport(
    "classic-expenses",
    scope,
    metricsTable([
      { metric: "Active expenses", value: data.activeExpenseTotal },
      { metric: "Voided expenses", value: data.voidedExpenseTotal },
      { metric: "Active count", value: data.activeExpenseCount },
      { metric: "Voided count", value: data.voidedExpenseCount },
    ]),
  );
}

export function triggerReportCsvDownload(result: ReportExportResult): void {
  downloadCsvFile(result.filename, result.csvText);
}
