import { useMemo, useState } from "react";
import { useLocation } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  formatReportPaymentMethod,
  getExpensesReport,
  getInventoryReport,
  getSalesReport,
  getUtangReport,
} from "@/api/pos/pos-reporting-client";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { ReportFilters } from "@/features/reports/ReportFilters";
import { type ClassicReportKind } from "@/features/reports/report-access";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function kindFromPath(pathname: string): ClassicReportKind {
  if (pathname.endsWith("/utang")) {
    return "utang";
  }
  if (pathname.endsWith("/inventory")) {
    return "inventory";
  }
  if (pathname.endsWith("/expenses")) {
    return "expenses";
  }
  return "sales";
}

export function ClassicReportPage() {
  const location = useLocation();
  const kind = kindFromPath(location.pathname);
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const [preset, setPreset] = useState<ReportDatePreset>("today");
  const [custom, setCustom] = useState<ReportDateRangeValue>(() =>
    resolveReportDatePreset("today"),
  );
  const [applied, setApplied] = useState<ReportDateRangeValue>(() =>
    resolveReportDatePreset("today"),
  );

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

  const titleKeys: Record<ClassicReportKind, MessageKey> = {
    sales: "reports.classicSales",
    utang: "reports.classicUtang",
    inventory: "reports.classicInventory",
    expenses: "reports.classicExpenses",
  };

  const query = useQuery({
    queryKey: [
      "classic-report",
      kind,
      workspace?.organizationId,
      workspace?.branchId,
      applied.fromDate,
      applied.toDate,
    ],
    enabled: Boolean(workspace),
    queryFn: async ({ signal }) => {
      if (kind === "sales") {
        const d = await getSalesReport(workspace!, applied, signal);
        return [
          {
            label: t("reports.metric.gross"),
            value: <MoneyDisplay amount={d.completedSalesTotal} />,
          },
          {
            label: t("reports.metric.transactions"),
            value: String(d.completedSaleCount),
          },
          {
            label: t("reports.metric.voids"),
            value: (
              <>
                <MoneyDisplay amount={d.voidedSalesTotal} /> ({d.voidedSaleCount})
              </>
            ),
          },
          {
            label: t("reports.metric.utangSales"),
            value: (
              <>
                <MoneyDisplay amount={d.utangSalesTotal} /> ({d.utangSaleCount})
              </>
            ),
          },
          {
            label: t("reports.metric.commercialDiscountNote"),
            value: t("reports.commercialDiscountUnavailable"),
          },
          ...d.byPaymentMethod.map((row) => ({
            label: formatReportPaymentMethod(row.paymentMethod),
            value: (
              <>
                <MoneyDisplay amount={row.amount} /> ({row.count})
              </>
            ),
          })),
        ];
      }
      if (kind === "utang") {
        const d = await getUtangReport(workspace!, applied, signal);
        return [
          {
            label: t("reports.metric.outstanding"),
            value: <MoneyDisplay amount={d.activeCustomerOutstanding} />,
          },
          {
            label: t("reports.metric.overdue"),
            value: <MoneyDisplay amount={d.overdueAmount} />,
          },
          {
            label: t("reports.metric.utangSales"),
            value: <MoneyDisplay amount={d.productBasedUtangSalesInPeriod} />,
          },
        ];
      }
      if (kind === "inventory") {
        const d = await getInventoryReport(workspace!, applied, signal);
        return [
          { label: t("reports.metric.tracked"), value: String(d.trackedProductCount) },
          { label: t("reports.metric.lowStock"), value: String(d.lowStockProductCount) },
          { label: t("reports.metric.outOfStock"), value: String(d.outOfStockProductCount) },
        ];
      }
      const d = await getExpensesReport(workspace!, applied, signal);
      return [
        {
          label: t("reports.metric.expenses"),
          value: <MoneyDisplay amount={d.activeExpenseTotal} />,
        },
        {
          label: t("reports.metric.voidedExpenses"),
          value: <MoneyDisplay amount={d.voidedExpenseTotal} />,
        },
      ];
    },
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const errorMessage = query.isError
    ? describePosApiError(query.error, t, "reports.loadError")
    : null;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="classic-report-page" data-kind={kind}>
      <PageHeader
        title={t(titleKeys[kind])}
        description={t("reports.classicLede")}
        backTo={pageBackNav.reports.to}
        backLabel={t(pageBackNav.reports.labelKey)}
        backTestId="page-header-back-reports"
      />

      <ReportFilters
        preset={preset}
        range={applied}
        custom={custom}
        branchLabel={branchLabel}
        onPresetChange={(next) => {
          setPreset(next);
          if (next !== "custom") {
            const range = resolveReportDatePreset(next);
            setCustom(range);
            setApplied(range);
          }
        }}
        onCustomChange={setCustom}
        onApply={() => setApplied(resolveReportDatePreset(preset, new Date(), custom))}
        loading={query.isFetching}
      />

      <Card data-testid="report-results">
        {query.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {errorMessage ? <ErrorState title={t("reports.errorTitle")} detail={errorMessage} /> : null}
        {query.data ? (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {query.data.map((line, index) => (
              <li
                key={`${String(line.label)}-${index}`}
                className="flex min-w-0 items-start justify-between gap-3 border-b border-border pb-2"
              >
                <span className="text-[length:var(--exits-text-sm)]">{line.label}</span>
                <span className="text-right text-[length:var(--exits-text-sm)]">{line.value}</span>
              </li>
            ))}
          </ul>
        ) : null}
      </Card>
    </div>
  );
}
