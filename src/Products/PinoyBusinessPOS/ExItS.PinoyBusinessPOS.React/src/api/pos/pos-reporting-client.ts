import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const DASHBOARD_PATH = "/api/v1/pos/dashboard";
const MANAGEMENT_OVERVIEW_PATH = "/api/v1/pos/management/overview";
const REPORTS_PATH = "/api/v1/pos/reports";

/** Date-only strings as `yyyy-MM-dd` (server ReportDateRange / UTC calendar day). */
const dateOnlySchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

const reportDailyAmountSchema = z.object({
  date: dateOnlySchema,
  amount: z.number(),
  count: z.number(),
});

const reportPaymentBreakdownSchema = z.object({
  paymentMethod: z.string(),
  amount: z.number(),
  count: z.number(),
});

const reportPeriodComparisonSchema = z.object({
  comparisonFromDate: dateOnlySchema,
  comparisonToDate: dateOnlySchema,
  absoluteChange: z.number().nullable().optional(),
  percentageChange: z.number().nullable().optional(),
  percentageAvailable: z.boolean(),
});

export const posManagementOverviewDtoSchema = z.object({
  businessDate: dateOnlySchema,
  todaySalesTotal: z.number(),
  todaySaleCount: z.number(),
  todayCashSalesTotal: z.number(),
  todayUtangSalesTotal: z.number(),
  todayPaymentsReceived: z.number(),
  openUtangOutstanding: z.number(),
  lowStockProductCount: z.number(),
  expiredLotCount: z.number(),
  nearExpiryLotCount: z.number(),
  pendingTransferCount: z.number(),
  openShiftCount: z.number(),
  activeRegisterCount: z.number(),
});

export const posDashboardDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  completedSalesTotal: z.number(),
  completedSaleCount: z.number(),
  cashSalesTotal: z.number(),
  manualGCashSalesTotal: z.number(),
  utangSalesTotal: z.number(),
  activeCustomerUtangOutstanding: z.number(),
  overdueUtangAmount: z.number(),
  recordedExpenseTotal: z.number(),
  lowStockProductCount: z.number(),
  voidedSaleCount: z.number(),
  voidedExpenseCount: z.number(),
  salesByDay: z.array(reportDailyAmountSchema),
  expensesByDay: z.array(reportDailyAmountSchema),
  paymentMethodBreakdown: z.array(reportPaymentBreakdownSchema),
  salesCountByDay: z.array(reportDailyAmountSchema),
  salesTotalComparison: reportPeriodComparisonSchema.nullable().optional(),
  expenseTotalComparison: reportPeriodComparisonSchema.nullable().optional(),
  commercialDiscountTotal: z.number().optional().default(0),
  preDiscountGrossSales: z.number().optional().default(0),
});

export const posSalesReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  completedSalesTotal: z.number(),
  completedSaleCount: z.number(),
  voidedSalesTotal: z.number(),
  voidedSaleCount: z.number(),
  utangSalesTotal: z.number(),
  utangSaleCount: z.number(),
  byPaymentMethod: z.array(reportPaymentBreakdownSchema),
  byProduct: z.array(z.record(z.string(), z.unknown())).optional(),
  byCategory: z.array(z.record(z.string(), z.unknown())).optional(),
  topProductsByQuantity: z.array(z.record(z.string(), z.unknown())).optional(),
  topProductsBySalesAmount: z.array(z.record(z.string(), z.unknown())).optional(),
  byDay: z.array(reportDailyAmountSchema).optional(),
  preDiscountGrossSales: z.number().optional().default(0),
  commercialDiscountTotal: z.number().optional().default(0),
  netSubtotal: z.number().optional().default(0),
  taxAmount: z.number().optional().default(0),
});

export const posInventoryReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  trackedProductCount: z.number(),
  lowStockProductCount: z.number(),
  outOfStockProductCount: z.number(),
  latestMovementAtUtc: z.string().nullable().optional(),
  movementsByType: z.array(
    z.object({
      movementType: z.string(),
      quantityTotal: z.number(),
      count: z.number(),
    }),
  ),
  trackedProducts: z.array(z.record(z.string(), z.unknown())).optional(),
  lowStockProducts: z.array(z.record(z.string(), z.unknown())).optional(),
  outOfStockProducts: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posUtangReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  activeCustomerOutstanding: z.number(),
  overdueAmount: z.number(),
  customersWithBalances: z.number(),
  customersWithOverdue: z.number(),
  creditsRecordedInPeriod: z.number(),
  creditsRecordedCount: z.number(),
  repaymentsRecordedInPeriod: z.number(),
  repaymentsRecordedCount: z.number(),
  productBasedUtangSalesInPeriod: z.number(),
  productBasedUtangSaleCount: z.number(),
  customersWithBalancesList: z.array(z.record(z.string(), z.unknown())).optional(),
  customersWithOverdueList: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posExpensesReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  activeExpenseTotal: z.number(),
  voidedExpenseTotal: z.number(),
  activeExpenseCount: z.number(),
  voidedExpenseCount: z.number(),
  byPaymentMethod: z.array(reportPaymentBreakdownSchema),
  byCategory: z.array(z.record(z.string(), z.unknown())).optional(),
  byDay: z.array(reportDailyAmountSchema).optional(),
  details: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posOperationalOverviewDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  completedGrossSales: z.number(),
  voidedSales: z.number(),
  refunds: z.number(),
  netSales: z.number(),
  completedTransactionCount: z.number(),
  averageTransactionValue: z.number(),
  preDiscountGrossSales: z.number().optional().default(0),
  commercialDiscountTotal: z.number().optional().default(0),
  netSubtotal: z.number().optional().default(0),
  taxAmount: z.number().optional().default(0),
});

export const posSalesSummaryReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  completedGrossSales: z.number(),
  voidedSales: z.number(),
  completedReturnsRefunds: z.number(),
  netSales: z.number(),
  completedTransactionCount: z.number(),
  averageTransactionValue: z.number(),
  preDiscountGrossSales: z.number().optional().default(0),
  commercialDiscountTotal: z.number().optional().default(0),
  netSubtotal: z.number().optional().default(0),
  taxAmount: z.number().optional().default(0),
});

export const posPaymentMethodBreakdownDtoSchema = z.object({
  paymentMethod: z.string(),
  grossCompleted: z.number(),
  voided: z.number(),
  refunded: z.number(),
  net: z.number(),
  preDiscountGross: z.number().optional().default(0),
  commercialDiscountTotal: z.number().optional().default(0),
});

export const posSalesByPaymentReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  rows: z.array(posPaymentMethodBreakdownDtoSchema),
});

export const posReturnsReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  returnCount: z.number(),
  returnedQuantity: z.number(),
  refundAmount: z.number(),
  byRefundMethod: z.array(z.record(z.string(), z.unknown())).optional(),
  byReason: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posInventoryStatusReportDtoSchema = z.object({
  asOfDate: dateOnlySchema,
  trackedCount: z.number(),
  lowStockCount: z.number(),
  outOfStockCount: z.number(),
  rows: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posInventoryMovementsReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  movementCount: z.number(),
  byType: z.array(
    z.object({
      movementType: z.string(),
      quantityTotal: z.number(),
      count: z.number(),
    }),
  ),
  rows: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posPurchasingSummaryReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  orderCount: z.number(),
  orderedQuantity: z.number(),
  receivedQuantity: z.number(),
  outstandingQuantity: z.number(),
  byStatus: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posPurchaseOutstandingReportDtoSchema = z.object({
  asOfDate: dateOnlySchema,
  outstandingOrderCount: z.number(),
  outstandingQuantity: z.number(),
  rows: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posShiftSummaryReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  shiftCount: z.number(),
  totalCashVariance: z.number(),
  rows: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posCashVarianceReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  closedShiftCount: z.number(),
  totalAbsoluteVariance: z.number(),
  totalSignedVariance: z.number(),
  rows: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posExpenseSummaryReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  recordedTotal: z.number(),
  voidedTotal: z.number(),
  recordedCount: z.number(),
  voidedCount: z.number(),
  byCategory: z.array(z.record(z.string(), z.unknown())).optional(),
  byPaymentMethod: z.array(reportPaymentBreakdownSchema).optional(),
});

export const posProductUtangSummaryReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  utangSalesTotal: z.number(),
  utangSaleCount: z.number(),
  outstandingTotal: z.number(),
  overdueTotal: z.number(),
  byProduct: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posSalesByProductReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  rows: z.array(
    z.object({
      productId: z.string(),
      productName: z.string(),
      unitOfMeasure: z.string(),
      sellingMode: z.string(),
      quantitySold: z.number(),
      quantityReturned: z.number(),
      netQuantity: z.number(),
      grossSaleAmount: z.number(),
      refundAmount: z.number(),
      netAmount: z.number(),
      preDiscountGrossSaleAmount: z.number().optional().default(0),
      commercialDiscountAmount: z.number().optional().default(0),
    }),
  ),
});

export const posStockCountVarianceReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  completedCount: z.number(),
  varianceLineCount: z.number(),
  rows: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posSupplierPurchasingReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  rows: z.array(z.record(z.string(), z.unknown())).optional(),
});

export const posProfitabilityReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  branchId: guidSchema.nullable().optional(),
  netSales: z.number(),
  cogsStatus: z.string(),
  knownCogs: z.number(),
  totalCogs: z.number().nullable().optional(),
  grossProfit: z.number().nullable().optional(),
  grossMarginPercent: z.number().nullable().optional(),
  completedSaleCount: z.number(),
  completeCostSaleCount: z.number(),
  partialCostSaleCount: z.number(),
  unavailableCostSaleCount: z.number(),
  wasteLossKnownCost: z.number(),
  wasteLossCostStatus: z.string(),
  stockUseKnownCost: z.number(),
  stockUseCostStatus: z.string(),
  costCompletenessPercent: z.number(),
  commercialDiscountTotal: z.number().optional().default(0),
});

export const posProductProfitabilityRowDtoSchema = z.object({
  productId: guidSchema,
  productName: z.string(),
  sku: z.string().nullable().optional(),
  unitOfMeasure: z.string(),
  quantitySold: z.number(),
  quantityReturned: z.number(),
  netQuantity: z.number(),
  salesBeforeDiscounts: z.number(),
  commercialDiscounts: z.number(),
  netSales: z.number(),
  refundAmount: z.number(),
  knownCogs: z.number(),
  cogsStatus: z.string(),
  totalCogs: z.number().nullable().optional(),
  grossProfit: z.number().nullable().optional(),
  grossMarginPercent: z.number().nullable().optional(),
  costCompletenessPercent: z.number(),
});

export const posProductProfitabilityReportDtoSchema = z.object({
  fromDate: dateOnlySchema,
  toDate: dateOnlySchema,
  branchId: guidSchema.nullable().optional(),
  rankBy: z.string(),
  rows: z.array(posProductProfitabilityRowDtoSchema),
});

export type PosManagementOverviewDto = z.infer<typeof posManagementOverviewDtoSchema>;
export type PosDashboardDto = z.infer<typeof posDashboardDtoSchema>;
export type PosSalesReportDto = z.infer<typeof posSalesReportDtoSchema>;
export type PosInventoryReportDto = z.infer<typeof posInventoryReportDtoSchema>;
export type PosUtangReportDto = z.infer<typeof posUtangReportDtoSchema>;
export type PosExpensesReportDto = z.infer<typeof posExpensesReportDtoSchema>;
export type PosOperationalOverviewDto = z.infer<typeof posOperationalOverviewDtoSchema>;
export type PosSalesSummaryReportDto = z.infer<typeof posSalesSummaryReportDtoSchema>;
export type PosSalesByPaymentReportDto = z.infer<typeof posSalesByPaymentReportDtoSchema>;
export type PosReturnsReportDto = z.infer<typeof posReturnsReportDtoSchema>;
export type PosInventoryStatusReportDto = z.infer<typeof posInventoryStatusReportDtoSchema>;
export type PosInventoryMovementsReportDto = z.infer<typeof posInventoryMovementsReportDtoSchema>;
export type PosPurchasingSummaryReportDto = z.infer<typeof posPurchasingSummaryReportDtoSchema>;
export type PosPurchaseOutstandingReportDto = z.infer<typeof posPurchaseOutstandingReportDtoSchema>;
export type PosShiftSummaryReportDto = z.infer<typeof posShiftSummaryReportDtoSchema>;
export type PosCashVarianceReportDto = z.infer<typeof posCashVarianceReportDtoSchema>;
export type PosExpenseSummaryReportDto = z.infer<typeof posExpenseSummaryReportDtoSchema>;
export type PosProductUtangSummaryReportDto = z.infer<typeof posProductUtangSummaryReportDtoSchema>;
export type PosSalesByProductReportDto = z.infer<typeof posSalesByProductReportDtoSchema>;
export type PosStockCountVarianceReportDto = z.infer<typeof posStockCountVarianceReportDtoSchema>;
export type PosSupplierPurchasingReportDto = z.infer<typeof posSupplierPurchasingReportDtoSchema>;
export type PosProfitabilityReportDto = z.infer<typeof posProfitabilityReportDtoSchema>;
export type PosProductProfitabilityReportDto = z.infer<
  typeof posProductProfitabilityReportDtoSchema
>;
export type PosProductProfitabilityRowDto = z.infer<typeof posProductProfitabilityRowDtoSchema>;

export type ReportDateQuery = {
  fromDate: string;
  toDate: string;
};

function appendDates(params: URLSearchParams, range?: ReportDateQuery | null) {
  if (!range) {
    return;
  }
  params.set("fromDate", range.fromDate);
  params.set("toDate", range.toDate);
}

function withQuery(
  path: string,
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  const params = new URLSearchParams();
  if (range) {
    appendDates(params, range);
  }
  if (branchId) {
    params.set("branchId", branchId);
  }
  const qs = params.toString();
  return qs ? `${path}?${qs}` : path;
}

export function managementOverviewPath(): string {
  return MANAGEMENT_OVERVIEW_PATH;
}

export function dashboardPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(DASHBOARD_PATH, range, branchId);
}

export function salesReportPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/sales`, range, branchId);
}

export function inventoryReportPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/inventory`, range);
}

export function utangReportPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/utang`, range);
}

export function expensesReportPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/expenses`, range);
}

export function operationalOverviewPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/overview`, range, branchId);
}

export function salesSummaryPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/sales-summary`, range, branchId);
}

export function salesByPaymentPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/sales-by-payment`, range, branchId);
}

export function salesByProductPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/sales-by-product`, range, branchId);
}

export function returnsReportPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/returns`, range, branchId);
}

export function shiftsSummaryPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/shifts-summary`, range);
}

export function cashVariancePath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/cash-variance`, range);
}

export function inventoryStatusPath(): string {
  return `${REPORTS_PATH}/inventory-status`;
}

export function inventoryMovementsPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/inventory-movements`, range, branchId);
}

export function stockCountVariancePath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/stock-count-variance`, range);
}

export function purchasingSummaryPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/purchasing-summary`, range);
}

export function purchaseOutstandingPath(): string {
  return `${REPORTS_PATH}/purchase-outstanding`;
}

export function supplierPurchasingPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/supplier-purchasing`, range);
}

export function expensesSummaryPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/expenses-summary`, range);
}

export function utangByProductPath(range?: ReportDateQuery | null): string {
  return withQuery(`${REPORTS_PATH}/utang-by-product`, range);
}

export function profitabilityPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
): string {
  return withQuery(`${REPORTS_PATH}/profitability`, range, branchId);
}

export function productProfitabilityPath(
  range?: ReportDateQuery | null,
  branchId?: string | null,
  rankBy?: string | null,
): string {
  const params = new URLSearchParams();
  if (range) {
    appendDates(params, range);
  }
  if (branchId) {
    params.set("branchId", branchId);
  }
  if (rankBy) {
    params.set("rankBy", rankBy);
  }
  const qs = params.toString();
  return qs ? `${REPORTS_PATH}/product-profitability?${qs}` : `${REPORTS_PATH}/product-profitability`;
}

/** Paths that must never appear as tax/VAT/BIR navigation targets in this package. */
export const TAX_REPORT_PATH_MARKERS = [
  "/tax",
  "/vat",
  "/bir",
  "tax-report",
  "vat-report",
  "bir-report",
] as const;

/** Fake P&L / COGS routes are not part of the proven report surface. */
export const FAKE_PL_PATH_MARKERS = ["/profit-loss", "/pnl", "/cogs", "/valuation"] as const;

/** Buyer purchase-history projection (RMAP-B04) is not seller reporting. */
export const BUYER_PURCHASE_PROJECTION_PATH_MARKERS = [
  "/personal/purchase-history",
  "/buyer/purchases",
  "/my-purchases",
] as const;

export async function getManagementOverview(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosManagementOverviewDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: managementOverviewPath(),
    workspace,
    signal,
  });
  return posManagementOverviewDtoSchema.parse(raw);
}

export async function getDashboard(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosDashboardDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: dashboardPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posDashboardDtoSchema.parse(raw);
}

export async function getSalesReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosSalesReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: salesReportPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posSalesReportDtoSchema.parse(raw);
}

export async function getInventoryReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosInventoryReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: inventoryReportPath(range),
    workspace,
    signal,
  });
  return posInventoryReportDtoSchema.parse(raw);
}

export async function getUtangReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosUtangReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: utangReportPath(range),
    workspace,
    signal,
  });
  return posUtangReportDtoSchema.parse(raw);
}

export async function getExpensesReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosExpensesReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: expensesReportPath(range),
    workspace,
    signal,
  });
  return posExpensesReportDtoSchema.parse(raw);
}

export async function getOperationalOverview(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosOperationalOverviewDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: operationalOverviewPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posOperationalOverviewDtoSchema.parse(raw);
}

export async function getSalesSummaryReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosSalesSummaryReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: salesSummaryPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posSalesSummaryReportDtoSchema.parse(raw);
}

export async function getSalesByPaymentReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosSalesByPaymentReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: salesByPaymentPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posSalesByPaymentReportDtoSchema.parse(raw);
}

export async function getSalesByProductReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosSalesByProductReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: salesByProductPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posSalesByProductReportDtoSchema.parse(raw);
}

export async function getReturnsReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosReturnsReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: returnsReportPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posReturnsReportDtoSchema.parse(raw);
}

export async function getShiftSummaryReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosShiftSummaryReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: shiftsSummaryPath(range),
    workspace,
    signal,
  });
  return posShiftSummaryReportDtoSchema.parse(raw);
}

export async function getCashVarianceReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosCashVarianceReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: cashVariancePath(range),
    workspace,
    signal,
  });
  return posCashVarianceReportDtoSchema.parse(raw);
}

export async function getInventoryStatusReport(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosInventoryStatusReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: inventoryStatusPath(),
    workspace,
    signal,
  });
  return posInventoryStatusReportDtoSchema.parse(raw);
}

export async function getInventoryMovementsReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosInventoryMovementsReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: inventoryMovementsPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posInventoryMovementsReportDtoSchema.parse(raw);
}

export async function getStockCountVarianceReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosStockCountVarianceReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: stockCountVariancePath(range),
    workspace,
    signal,
  });
  return posStockCountVarianceReportDtoSchema.parse(raw);
}

export async function getPurchasingSummaryReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosPurchasingSummaryReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: purchasingSummaryPath(range),
    workspace,
    signal,
  });
  return posPurchasingSummaryReportDtoSchema.parse(raw);
}

export async function getPurchaseOutstandingReport(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosPurchaseOutstandingReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: purchaseOutstandingPath(),
    workspace,
    signal,
  });
  return posPurchaseOutstandingReportDtoSchema.parse(raw);
}

export async function getSupplierPurchasingReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosSupplierPurchasingReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: supplierPurchasingPath(range),
    workspace,
    signal,
  });
  return posSupplierPurchasingReportDtoSchema.parse(raw);
}

export async function getExpenseSummaryReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosExpenseSummaryReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: expensesSummaryPath(range),
    workspace,
    signal,
  });
  return posExpenseSummaryReportDtoSchema.parse(raw);
}

export async function getProductUtangSummaryReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
): Promise<PosProductUtangSummaryReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: utangByProductPath(range),
    workspace,
    signal,
  });
  return posProductUtangSummaryReportDtoSchema.parse(raw);
}

export async function getProfitabilityReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
): Promise<PosProfitabilityReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: profitabilityPath(range, reportBranchId),
    workspace,
    signal,
  });
  return posProfitabilityReportDtoSchema.parse(raw);
}

export async function getProductProfitabilityReport(
  workspace: PosWorkspaceScope,
  range: ReportDateQuery,
  signal?: AbortSignal,
  reportBranchId?: string | null,
  rankBy?: string | null,
): Promise<PosProductProfitabilityReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    path: productProfitabilityPath(range, reportBranchId, rankBy),
    workspace,
    signal,
  });
  return posProductProfitabilityReportDtoSchema.parse(raw);
}

/** Friendly payment label — never tax terminology. */
export function formatReportPaymentMethod(code: string): string {
  const normalized = code.trim().toLowerCase();
  if (normalized === "cash") {
    return "Cash";
  }
  if (normalized === "manualgcash" || normalized === "gcash") {
    return "GCash";
  }
  if (normalized === "utang") {
    return "Utang";
  }
  return code;
}
