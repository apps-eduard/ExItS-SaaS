import { useMemo, useState } from "react";
import { Navigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  formatReportPaymentMethod,
  getCashVarianceReport,
  getExpenseSummaryReport,
  getInventoryMovementsReport,
  getInventoryStatusReport,
  getOperationalOverview,
  getProductUtangSummaryReport,
  getPurchaseOutstandingReport,
  getPurchasingSummaryReport,
  getReturnsReport,
  getSalesByPaymentReport,
  getSalesByProductReport,
  getSalesSummaryReport,
  getShiftSummaryReport,
  getStockCountVarianceReport,
  getSupplierPurchasingReport,
} from "@/api/pos/pos-reporting-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { ReportFilters } from "@/features/reports/ReportFilters";
import {
  canAccessOperationalReport,
  isOperationalReportKind,
  operationalReportNeedsDates,
  type OperationalReportKind,
} from "@/features/reports/report-access";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Line = { label: string; value: React.ReactNode };

function titleKeyFor(kind: OperationalReportKind): MessageKey {
  const map: Record<OperationalReportKind, MessageKey> = {
    overview: "reports.overview",
    "sales-summary": "reports.salesSummary",
    "sales-by-payment": "reports.salesByPayment",
    "sales-by-product": "reports.salesByProduct",
    returns: "reports.returns",
    shifts: "reports.shiftSummary",
    "cash-variance": "reports.cashVariance",
    "inventory-status": "reports.inventoryStatus",
    "inventory-movements": "reports.inventoryMovements",
    "stock-count-variance": "reports.stockCountVariance",
    "purchasing-summary": "reports.purchasingSummary",
    "purchase-outstanding": "reports.purchaseOutstanding",
    "supplier-purchasing": "reports.supplierPurchasing",
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
): Promise<Line[]> {
  switch (kind) {
    case "overview": {
      const d = await getOperationalOverview(workspace, range, signal);
      return [
        {
          label: t("reports.metric.gross"),
          value: <MoneyDisplay amount={d.completedGrossSales} />,
        },
        { label: t("reports.metric.voids"), value: <MoneyDisplay amount={d.voidedSales} /> },
        { label: t("reports.metric.returns"), value: <MoneyDisplay amount={d.refunds} /> },
        { label: t("reports.metric.net"), value: <MoneyDisplay amount={d.netSales} /> },
        {
          label: t("reports.metric.transactions"),
          value: String(d.completedTransactionCount),
        },
        {
          label: t("reports.metric.avgTxn"),
          value: <MoneyDisplay amount={d.averageTransactionValue} />,
        },
        {
          label: t("reports.metric.commercialDiscountNote"),
          value: t("reports.commercialDiscountUnavailable"),
        },
      ];
    }
    case "sales-summary": {
      const d = await getSalesSummaryReport(workspace, range, signal);
      return [
        {
          label: t("reports.metric.gross"),
          value: <MoneyDisplay amount={d.completedGrossSales} />,
        },
        { label: t("reports.metric.voids"), value: <MoneyDisplay amount={d.voidedSales} /> },
        {
          label: t("reports.metric.returns"),
          value: <MoneyDisplay amount={d.completedReturnsRefunds} />,
        },
        { label: t("reports.metric.net"), value: <MoneyDisplay amount={d.netSales} /> },
        {
          label: t("reports.metric.transactions"),
          value: String(d.completedTransactionCount),
        },
        {
          label: t("reports.metric.commercialDiscountNote"),
          value: t("reports.commercialDiscountUnavailable"),
        },
      ];
    }
    case "sales-by-payment": {
      const d = await getSalesByPaymentReport(workspace, range, signal);
      return d.rows.flatMap((row) => [
        {
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.gross")}`,
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
          label: `${formatReportPaymentMethod(row.paymentMethod)} — ${t("reports.metric.net")}`,
          value: <MoneyDisplay amount={row.net} />,
        },
      ]);
    }
    case "sales-by-product": {
      const d = await getSalesByProductReport(workspace, range, signal);
      return d.rows.slice(0, 25).map((row) => ({
        label: row.productName,
        value: <MoneyDisplay amount={row.netAmount} />,
      }));
    }
    case "returns": {
      const d = await getReturnsReport(workspace, range, signal);
      return [
        { label: t("reports.metric.returnCount"), value: String(d.returnCount) },
        { label: t("reports.metric.returnedQty"), value: String(d.returnedQuantity) },
        {
          label: t("reports.metric.refundAmount"),
          value: <MoneyDisplay amount={d.refundAmount} />,
        },
      ];
    }
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
      const d = await getInventoryMovementsReport(workspace, range, signal);
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

  const kindValid = Boolean(kindParam && isOperationalReportKind(kindParam));
  const kind = kindValid ? (kindParam as OperationalReportKind) : "overview";
  const allowed = kindValid && canAccessOperationalReport(sessionGrant, kind);
  const needsDates = operationalReportNeedsDates(kind);

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

  const branchLabel = boundWorkspace
    ? boundWorkspace.branchName
      ? `${boundWorkspace.organizationDisplayName} · ${boundWorkspace.branchName}`
      : boundWorkspace.organizationDisplayName
    : t("reports.noBranch");

  const query = useQuery({
    queryKey: [
      "operational-report",
      kind,
      workspace?.organizationId,
      workspace?.branchId,
      applied.fromDate,
      applied.toDate,
    ],
    enabled: Boolean(workspace && kindValid && allowed),
    queryFn: ({ signal }) => loadLines(kind, workspace!, applied, signal, t),
  });

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

  const errorMessage = query.isError
    ? describePosApiError(query.error, t, "reports.loadError")
    : null;

  return (
    <div
      className="flex min-w-0 flex-col gap-4"
      data-testid="operational-report-page"
      data-kind={kind}
    >
      <PageHeader
        title={t(titleKeyFor(kind))}
        description={t("reports.operationalLede")}
        backTo={pageBackNav.reports.to}
        backLabel={t(pageBackNav.reports.labelKey)}
        backTestId="page-header-back-reports"
      />

      <ReportFilters
        preset={preset}
        range={applied}
        custom={custom}
        branchLabel={branchLabel}
        onPresetChange={onPresetChange}
        onCustomChange={setCustom}
        onApply={() => setApplied(resolveReportDatePreset(preset, new Date(), custom))}
        loading={query.isFetching}
        showDates={needsDates}
      />

      <Button
        type="button"
        variant="ghost"
        className="min-h-11 w-fit"
        data-testid="report-refresh"
        disabled={query.isFetching}
        onClick={() => void query.refetch()}
      >
        {t("dashboard.refresh")}
      </Button>

      <Card data-testid="report-results">
        {query.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {errorMessage ? <ErrorState title={t("reports.errorTitle")} detail={errorMessage} /> : null}
        {query.data && query.data.length > 0 ? (
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
        {query.data && query.data.length === 0 && !query.isLoading && !errorMessage ? (
          <p className="m-0 text-muted">{t("reports.emptyDetail")}</p>
        ) : null}
      </Card>
    </div>
  );
}
