import { useEffect, useMemo, useState } from "react";
import { Navigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  hasOrganizationManagementAuthority,
  isPosOperationsManager,
  isPosOwnerRole,
  resolveEffectivePosRoleCode,
} from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  formatReportPaymentMethod,
  getCashVarianceReport,
  getExpenseSummaryReport,
  getInventoryMovementsReport,
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
  getSalesSummaryReport,
  getShiftSummaryReport,
  getStockCountVarianceReport,
  getSupplierPayablesReport,
  getSupplierPurchasingReport,
} from "@/api/pos/pos-reporting-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { ReportCsvExportButton } from "@/features/reports/ReportCsvExportButton";
import { ReportFilters } from "@/features/reports/ReportFilters";
import { ReportScopeControls } from "@/features/reports/ReportScopeControls";
import {
  canAccessOperationalReport,
  isOperationalReportKind,
  operationalReportNeedsDates,
  type OperationalReportKind,
} from "@/features/reports/report-access";
import {
  buildOperationalReportExport,
  resolveReportExportScopeLabel,
} from "@/features/reports/report-csv-export";
import {
  canSelectAllBranches,
  reportScopeModeForOperational,
  resolveReportBranchIdQuery,
  type ReportBranchScopeSelection,
} from "@/features/reports/report-branch-scope";
import {
  ProductProfitabilityTable,
  type ProductProfitabilityRankBy,
} from "@/features/reports/ProductProfitabilityTable";
import { SupplierPayablesReportView } from "@/features/reports/SupplierPayablesReportView";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { productionCostStatusLabelKey } from "@/features/inventory/production-labels";

type Line = { label: string; value: React.ReactNode };

function formatMarginPercent(value: number): string {
  return `${value.toFixed(1)}%`;
}

function titleKeyFor(kind: OperationalReportKind): MessageKey {
  const map: Record<OperationalReportKind, MessageKey> = {
    overview: "reports.overview",
    "sales-summary": "reports.salesSummary",
    "sales-by-payment": "reports.salesByPayment",
    "sales-by-product": "reports.salesByProduct",
    returns: "reports.returns",
    profitability: "reports.profitability",
    "product-profitability": "reports.productProfitability",
    shifts: "reports.shiftSummary",
    "cash-variance": "reports.cashVariance",
    "inventory-status": "reports.inventoryStatus",
    "inventory-movements": "reports.inventoryMovements",
    "stock-count-variance": "reports.stockCountVariance",
    "purchasing-summary": "reports.purchasingSummary",
    "purchase-outstanding": "reports.purchaseOutstanding",
    "supplier-purchasing": "reports.supplierPurchasing",
    "supplier-payables": "reports.supplierPayables",
    "expenses-summary": "reports.expenseSummary",
    "utang-by-product": "reports.utangByProduct",
  };
  return map[kind];
}

async function loadLines(
  kind: OperationalReportKind,
  workspace: { organizationId: string; branchId?: string | null },
  range: ReportDateRangeValue,
  signal: AbortSignal,
  t: (key: MessageKey) => string,
  reportBranchId?: string | null,
): Promise<Line[]> {
  switch (kind) {
    case "overview": {
      const d = await getOperationalOverview(workspace, range, signal, reportBranchId);
      return [
        {
          label: t("reports.metric.preDiscountGross"),
          value: <MoneyDisplay amount={d.preDiscountGrossSales} />,
        },
        {
          label: t("reports.metric.commercialDiscounts"),
          value: <MoneyDisplay amount={d.commercialDiscountTotal} />,
        },
        {
          label: t("reports.metric.netSubtotal"),
          value: <MoneyDisplay amount={d.netSubtotal} />,
        },
        {
          label: t("reports.metric.tax"),
          value: <MoneyDisplay amount={d.taxAmount} />,
        },
        {
          label: t("reports.metric.completedSales"),
          value: <MoneyDisplay amount={d.completedGrossSales} />,
        },
        { label: t("reports.metric.voids"), value: <MoneyDisplay amount={d.voidedSales} /> },
        { label: t("reports.metric.returns"), value: <MoneyDisplay amount={d.refunds} /> },
        { label: t("reports.metric.netSales"), value: <MoneyDisplay amount={d.netSales} /> },
        {
          label: t("reports.metric.transactions"),
          value: String(d.completedTransactionCount),
        },
        {
          label: t("reports.metric.avgTxn"),
          value: <MoneyDisplay amount={d.averageTransactionValue} />,
        },
      ];
    }
    case "sales-summary": {
      const d = await getSalesSummaryReport(workspace, range, signal, reportBranchId);
      return [
        {
          label: t("reports.metric.preDiscountGross"),
          value: <MoneyDisplay amount={d.preDiscountGrossSales} />,
        },
        {
          label: t("reports.metric.commercialDiscounts"),
          value: <MoneyDisplay amount={d.commercialDiscountTotal} />,
        },
        {
          label: t("reports.metric.netSubtotal"),
          value: <MoneyDisplay amount={d.netSubtotal} />,
        },
        {
          label: t("reports.metric.tax"),
          value: <MoneyDisplay amount={d.taxAmount} />,
        },
        {
          label: t("reports.metric.completedSales"),
          value: <MoneyDisplay amount={d.completedGrossSales} />,
        },
        { label: t("reports.metric.voids"), value: <MoneyDisplay amount={d.voidedSales} /> },
        {
          label: t("reports.metric.returns"),
          value: <MoneyDisplay amount={d.completedReturnsRefunds} />,
        },
        { label: t("reports.metric.netSales"), value: <MoneyDisplay amount={d.netSales} /> },
        {
          label: t("reports.metric.transactions"),
          value: String(d.completedTransactionCount),
        },
      ];
    }
    case "sales-by-payment": {
      const d = await getSalesByPaymentReport(workspace, range, signal, reportBranchId);
      return d.rows.flatMap((row) => [
        {
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.preDiscountGross")}`,
          value: <MoneyDisplay amount={row.preDiscountGross} />,
        },
        {
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.commercialDiscounts")}`,
          value: <MoneyDisplay amount={row.commercialDiscountTotal} />,
        },
        {
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.completedSales")}`,
          value: <MoneyDisplay amount={row.grossCompleted} />,
        },
        {
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.voids")}`,
          value: <MoneyDisplay amount={row.voided} />,
        },
        {
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.returns")}`,
          value: <MoneyDisplay amount={row.refunded} />,
        },
        {
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.netSales")}`,
          value: <MoneyDisplay amount={row.net} />,
        },
      ]);
    }
    case "sales-by-product": {
      const d = await getSalesByProductReport(workspace, range, signal, reportBranchId);
      return d.rows.slice(0, 25).flatMap((row) => [
        {
          label: `${row.productName} — ${t("reports.metric.preDiscountGross")}`,
          value: <MoneyDisplay amount={row.preDiscountGrossSaleAmount} />,
        },
        {
          label: `${row.productName} — ${t("reports.metric.commercialDiscounts")}`,
          value: <MoneyDisplay amount={row.commercialDiscountAmount} />,
        },
        {
          label: `${row.productName} — ${t("reports.metric.netSales")}`,
          value: <MoneyDisplay amount={row.netAmount} />,
        },
      ]);
    }
    case "returns": {
      const d = await getReturnsReport(workspace, range, signal, reportBranchId);
      return [
        { label: t("reports.metric.returnCount"), value: String(d.returnCount) },
        { label: t("reports.metric.returnedQty"), value: String(d.returnedQuantity) },
        {
          label: t("reports.metric.refundAmount"),
          value: <MoneyDisplay amount={d.refundAmount} />,
        },
      ];
    }
    case "profitability": {
      const d = await getProfitabilityReport(workspace, range, signal, reportBranchId);
      const cogsComplete = d.cogsStatus === "Complete";
      const lines: Line[] = [
        { label: t("reports.metric.netSales"), value: <MoneyDisplay amount={d.netSales} /> },
        {
          label: t("reports.metric.commercialDiscounts"),
          value: <MoneyDisplay amount={d.commercialDiscountTotal} />,
        },
      ];

      if (cogsComplete && d.totalCogs != null) {
        lines.push({
          label: t("reports.metric.cogs"),
          value: <MoneyDisplay amount={d.totalCogs} />,
        });
        if (d.grossProfit != null) {
          lines.push({
            label: t("reports.metric.grossProfit"),
            value: <MoneyDisplay amount={d.grossProfit} />,
          });
        }
        if (d.grossMarginPercent != null) {
          lines.push({
            label: t("reports.metric.grossMargin"),
            value: formatMarginPercent(d.grossMarginPercent),
          });
        }
      } else {
        lines.push({
          label: t("reports.metric.knownCogs"),
          value: <MoneyDisplay amount={d.knownCogs} />,
        });
        lines.push({
          label: t("reports.metric.costIncomplete"),
          value:
            d.cogsStatus === "Partial"
              ? t("reports.costIncompletePartial")
              : t("reports.costIncompleteUnavailable"),
        });
      }

      lines.push(
        {
          label: t("reports.metric.wasteLossCost"),
          value: (
            <>
              <MoneyDisplay amount={d.wasteLossKnownCost} />
              <span className="ml-1 text-muted">
                ({t(productionCostStatusLabelKey(d.wasteLossCostStatus))})
              </span>
            </>
          ),
        },
        {
          label: t("reports.metric.stockUseCost"),
          value: (
            <>
              <MoneyDisplay amount={d.stockUseKnownCost} />
              <span className="ml-1 text-muted">
                ({t(productionCostStatusLabelKey(d.stockUseCostStatus))})
              </span>
            </>
          ),
        },
        {
          label: t("reports.metric.costCompleteness"),
          value: formatMarginPercent(d.costCompletenessPercent),
        },
        {
          label: t("reports.metric.completedSales"),
          value: String(d.completedSaleCount),
        },
      );

      return lines;
    }
    case "product-profitability":
      return [];
    case "shifts": {
      const d = await getShiftSummaryReport(workspace, range, signal);
      return [
        { label: t("reports.metric.shiftCount"), value: String(d.shiftCount) },
        {
          label: t("reports.metric.cashVariance"),
          value: <MoneyDisplay amount={d.totalCashVariance} />,
        },
      ];
    }
    case "cash-variance": {
      const d = await getCashVarianceReport(workspace, range, signal);
      return [
        { label: t("reports.metric.closedShifts"), value: String(d.closedShiftCount) },
        {
          label: t("reports.metric.absVariance"),
          value: <MoneyDisplay amount={d.totalAbsoluteVariance} />,
        },
        {
          label: t("reports.metric.signedVariance"),
          value: <MoneyDisplay amount={d.totalSignedVariance} />,
        },
      ];
    }
    case "inventory-status": {
      const d = await getInventoryStatusReport(workspace, signal);
      return [
        { label: t("reports.metric.tracked"), value: String(d.trackedCount) },
        { label: t("reports.metric.lowStock"), value: String(d.lowStockCount) },
        { label: t("reports.metric.outOfStock"), value: String(d.outOfStockCount) },
      ];
    }
    case "inventory-movements": {
      const d = await getInventoryMovementsReport(workspace, range, signal, reportBranchId);
      return [
        { label: t("reports.metric.movements"), value: String(d.movementCount) },
        ...d.byType.map((row) => ({
          label: row.movementType,
          value: `${row.count} / ${row.quantityTotal}`,
        })),
      ];
    }
    case "stock-count-variance": {
      const d = await getStockCountVarianceReport(workspace, range, signal);
      return [
        { label: t("reports.metric.completedCounts"), value: String(d.completedCount) },
        { label: t("reports.metric.varianceLines"), value: String(d.varianceLineCount) },
      ];
    }
    case "purchasing-summary": {
      const d = await getPurchasingSummaryReport(workspace, range, signal);
      return [
        { label: t("reports.metric.orders"), value: String(d.orderCount) },
        { label: t("reports.metric.orderedQty"), value: String(d.orderedQuantity) },
        { label: t("reports.metric.receivedQty"), value: String(d.receivedQuantity) },
        { label: t("reports.metric.outstandingQty"), value: String(d.outstandingQuantity) },
      ];
    }
    case "purchase-outstanding": {
      const d = await getPurchaseOutstandingReport(workspace, signal);
      return [
        { label: t("reports.metric.outstandingOrders"), value: String(d.outstandingOrderCount) },
        { label: t("reports.metric.outstandingQty"), value: String(d.outstandingQuantity) },
      ];
    }
    case "supplier-purchasing": {
      const d = await getSupplierPurchasingReport(workspace, range, signal);
      return (d.rows ?? []).slice(0, 25).map((row) => {
        const r = row as {
          supplierName?: string | null;
          orderedQuantity?: number;
        };
        return {
          label: r.supplierName ?? t("reports.unknownSupplier"),
          value: String(r.orderedQuantity ?? 0),
        };
      });
    }
    case "supplier-payables":
      return [];
    case "expenses-summary": {
      const d = await getExpenseSummaryReport(workspace, range, signal);
      return [
        { label: t("reports.metric.expenses"), value: <MoneyDisplay amount={d.recordedTotal} /> },
        {
          label: t("reports.metric.voidedExpenses"),
          value: <MoneyDisplay amount={d.voidedTotal} />,
        },
        { label: t("reports.metric.expenseCount"), value: String(d.recordedCount) },
      ];
    }
    case "utang-by-product": {
      const d = await getProductUtangSummaryReport(workspace, range, signal);
      return [
        {
          label: t("reports.metric.utangSales"),
          value: <MoneyDisplay amount={d.utangSalesTotal} />,
        },
        { label: t("reports.metric.utangCount"), value: String(d.utangSaleCount) },
        {
          label: t("reports.metric.outstanding"),
          value: <MoneyDisplay amount={d.outstandingTotal} />,
        },
        { label: t("reports.metric.overdue"), value: <MoneyDisplay amount={d.overdueTotal} /> },
      ];
    }
    default:
      return [];
  }
}

export function OperationalReportPage() {
  const { kind: kindParam } = useParams<{ kind: string }>();
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [preset, setPreset] = useState<ReportDatePreset>("today");
  const [custom, setCustom] = useState<ReportDateRangeValue>(() =>
    resolveReportDatePreset("today"),
  );
  const [applied, setApplied] = useState<ReportDateRangeValue>(() =>
    resolveReportDatePreset("today"),
  );
  const [scopeSelection, setScopeSelection] = useState<ReportBranchScopeSelection>({
    mode: "current",
  });
  const [rankBy, setRankBy] = useState<ProductProfitabilityRankBy>("grossProfitDesc");

  const kindValid = Boolean(kindParam && isOperationalReportKind(kindParam));
  const kind = kindValid ? (kindParam as OperationalReportKind) : "overview";
  const allowed = kindValid && canAccessOperationalReport(sessionGrant, kind);
  const needsDates = operationalReportNeedsDates(kind);
  const isProductProfitability = kind === "product-profitability";
  const isSupplierPayables = kind === "supplier-payables";
  const scopeMode = reportScopeModeForOperational(kind);
  const allowAll = canSelectAllBranches({
    hasOrgManagement: hasOrganizationManagementAuthority(sessionGrant),
    isOwner: isPosOwnerRole(sessionGrant),
    isManager: isPosOperationsManager(sessionGrant),
    isReportingUser: resolveEffectivePosRoleCode(sessionGrant)?.toLowerCase() === "reportinguser",
  });

  const workspace = useMemo(
    () =>
      boundWorkspace
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId,
          }
        : null,
    [boundWorkspace],
  );

  useEffect(() => {
    setScopeSelection({ mode: "current" });
  }, [workspace?.organizationId]);

  const reportBranchId = resolveReportBranchIdQuery(
    scopeMode,
    scopeSelection,
    workspace?.branchId,
  );

  const query = useQuery({
    queryKey: [
      "operational-report",
      kind,
      workspace?.organizationId,
      reportBranchId ?? "all",
      applied.fromDate,
      applied.toDate,
    ],
    enabled:
      Boolean(workspace && kindValid && allowed) &&
      !isProductProfitability &&
      !isSupplierPayables,
    queryFn: ({ signal }) => loadLines(kind, workspace!, applied, signal, t, reportBranchId),
  });

  const productProfitQuery = useQuery({
    queryKey: [
      "product-profitability",
      workspace?.organizationId,
      reportBranchId ?? "all",
      applied.fromDate,
      applied.toDate,
      rankBy,
    ],
    enabled: Boolean(workspace && kindValid && allowed && isProductProfitability),
    queryFn: ({ signal }) =>
      getProductProfitabilityReport(workspace!, applied, signal, reportBranchId, rankBy),
  });

  const supplierPayablesQuery = useQuery({
    queryKey: ["supplier-payables-report", workspace?.organizationId],
    enabled: Boolean(workspace && kindValid && allowed && isSupplierPayables),
    queryFn: ({ signal }) =>
      getSupplierPayablesReport(workspace!, { outstandingOnly: false }, signal),
  });

  const activeQuery = isProductProfitability
    ? productProfitQuery
    : isSupplierPayables
      ? supplierPayablesQuery
      : query;

  if (!kindValid || !allowed) {
    return <Navigate to="/reports" replace />;
  }

  function onPresetChange(next: ReportDatePreset) {
    setPreset(next);
    if (next !== "custom") {
      const range = resolveReportDatePreset(next);
      setCustom(range);
      setApplied(range);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const errorMessage = activeQuery.isError
    ? describePosApiError(activeQuery.error, t, "reports.loadError")
    : null;

  return (
    <div
      className="flex min-w-0 flex-col gap-4"
      data-testid="operational-report-page"
      data-kind={kind}
    >
      <PageHeader
        title={t(titleKeyFor(kind))}
        description={t(
          isProductProfitability
            ? "reports.productProfitabilityLede"
            : isSupplierPayables
              ? "reports.supplierPayables.lede"
              : "reports.operationalLede",
        )}
        backTo={pageBackNav.reports.to}
        backLabel={t(pageBackNav.reports.labelKey)}
        backTestId="page-header-back-reports"
      />

      <ReportFilters
        preset={preset}
        range={applied}
        custom={custom}
        scopeSlot={
          <ReportScopeControls
            scopeMode={scopeMode}
            organizationId={workspace.organizationId}
            currentBranchId={workspace.branchId}
            currentBranchName={boundWorkspace?.branchName}
            selection={scopeSelection}
            onSelectionChange={setScopeSelection}
            allowAllBranches={allowAll}
            loading={activeQuery.isFetching}
          />
        }
        onPresetChange={onPresetChange}
        onCustomChange={setCustom}
        onApply={() => setApplied(resolveReportDatePreset(preset, new Date(), custom))}
        loading={activeQuery.isFetching}
        showDates={needsDates}
      />

      <div className="flex min-w-0 flex-wrap items-start gap-2">
        <Button
          type="button"
          variant="ghost"
          className="w-fit"
          data-testid="report-refresh"
          disabled={activeQuery.isFetching}
          onClick={() => void activeQuery.refetch()}
        >
          {t("dashboard.refresh")}
        </Button>
        <ReportCsvExportButton
          disabled={activeQuery.isFetching}
          onExport={(signal) =>
            buildOperationalReportExport({
              kind,
              workspace,
              range: applied,
              reportBranchId,
              rankBy: isProductProfitability ? rankBy : null,
              productProfitabilityRows: isProductProfitability
                ? (productProfitQuery.data?.rows ?? null)
                : null,
              signal,
              scope: {
                organizationName: boundWorkspace?.organizationDisplayName,
                scopeLabel: resolveReportExportScopeLabel({
                  scopeMode,
                  selection: scopeSelection,
                  currentBranchName: boundWorkspace?.branchName,
                }),
                fromDate: needsDates
                  ? applied.fromDate
                  : isSupplierPayables
                    ? (supplierPayablesQuery.data?.asOfDate ?? null)
                    : null,
                toDate: needsDates
                  ? applied.toDate
                  : isSupplierPayables
                    ? (supplierPayablesQuery.data?.asOfDate ?? null)
                    : null,
              },
            })
          }
        />
      </div>

      <Card data-testid="report-results">
        {activeQuery.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {errorMessage ? <ErrorState title={t("reports.errorTitle")} detail={errorMessage} /> : null}
        {isProductProfitability && productProfitQuery.data ? (
          productProfitQuery.data.rows.length > 0 ? (
            <ProductProfitabilityTable
              rows={productProfitQuery.data.rows}
              rankBy={rankBy}
              onRankByChange={setRankBy}
            />
          ) : !productProfitQuery.isLoading && !errorMessage ? (
            <p className="m-0 text-muted">{t("reports.emptyDetail")}</p>
          ) : null
        ) : null}
        {isSupplierPayables && supplierPayablesQuery.data ? (
          <SupplierPayablesReportView report={supplierPayablesQuery.data} />
        ) : null}
        {!isProductProfitability &&
        !isSupplierPayables &&
        query.data &&
        query.data.length > 0 ? (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {query.data.map((line, index) => (
              <li
                key={`${line.label}-${index}`}
                className="flex min-w-0 items-start justify-between gap-3 border-b border-border pb-2"
              >
                <span className="text-[length:var(--exits-text-sm)]">{line.label}</span>
                <span className="text-right text-[length:var(--exits-text-sm)]">{line.value}</span>
              </li>
            ))}
          </ul>
        ) : null}
        {!isProductProfitability &&
        !isSupplierPayables &&
        query.data &&
        query.data.length === 0 &&
        !query.isLoading &&
        !errorMessage ? (
          <p className="m-0 text-muted">{t("reports.emptyDetail")}</p>
        ) : null}
      </Card>
    </div>
  );
}
