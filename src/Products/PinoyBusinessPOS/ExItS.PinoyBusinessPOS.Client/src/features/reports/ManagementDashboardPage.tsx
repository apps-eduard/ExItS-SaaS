import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  formatReportPaymentMethod,
  getDashboard,
  getManagementOverview,
} from "@/api/pos/pos-reporting-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { ReportFilters } from "@/features/reports/ReportFilters";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function MetricCard({
  label,
  children,
  meta,
  testId,
}: {
  label: string;
  children: React.ReactNode;
  meta?: React.ReactNode;
  testId: string;
}) {
  return (
    <div
      className="flex min-w-0 flex-col gap-1 rounded-[var(--exits-radius-md)] border border-border p-3"
      data-testid={testId}
      role="listitem"
    >
      <span className="text-[length:var(--exits-text-sm)] text-muted">{label}</span>
      <span className="text-[length:var(--exits-text-lg)] font-semibold">{children}</span>
      {meta ? <span className="text-[length:var(--exits-text-xs)] text-muted">{meta}</span> : null}
    </div>
  );
}

export function ManagementDashboardPage() {
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

  const overviewQuery = useQuery({
    queryKey: ["management-overview", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getManagementOverview(workspace!, signal),
  });

  const dashboardQuery = useQuery({
    queryKey: [
      "pos-dashboard",
      workspace?.organizationId,
      workspace?.branchId,
      applied.fromDate,
      applied.toDate,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getDashboard(workspace!, applied, signal),
  });

  function onPresetChange(next: ReportDatePreset) {
    setPreset(next);
    if (next !== "custom") {
      const range = resolveReportDatePreset(next);
      setCustom(range);
      setApplied(range);
    }
  }

  function onApply() {
    const range = resolveReportDatePreset(preset, new Date(), custom);
    setApplied(range);
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const overviewError = overviewQuery.isError
    ? describePosApiError(overviewQuery.error, t, "reports.loadError")
    : null;
  const dashboardError = dashboardQuery.isError
    ? describePosApiError(dashboardQuery.error, t, "reports.loadError")
    : null;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="management-dashboard-page">
      <PageHeader title={t("dashboard.title")} description={t("dashboard.lede")} />

      <div className="flex min-w-0 flex-wrap gap-2">
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-reports-hub">
          <Link to="/reports">{t("reports.open")}</Link>
        </Button>
        <Button
          type="button"
          variant="ghost"
          className="min-h-11 w-fit"
          data-testid="dashboard-refresh"
          disabled={overviewQuery.isFetching || dashboardQuery.isFetching}
          onClick={() => {
            void overviewQuery.refetch();
            void dashboardQuery.refetch();
          }}
        >
          {t("dashboard.refresh")}
        </Button>
      </div>

      <ReportFilters
        preset={preset}
        range={applied}
        custom={custom}
        branchLabel={branchLabel}
        onPresetChange={onPresetChange}
        onCustomChange={setCustom}
        onApply={onApply}
        loading={dashboardQuery.isFetching}
      />

      <Card data-testid="management-overview-panel">
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-md)] font-semibold">
          {t("dashboard.todayOverview")}
        </h2>
        {overviewQuery.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {overviewError ? (
          <ErrorState title={t("reports.errorTitle")} detail={overviewError} />
        ) : null}
        {overviewQuery.data ? (
          <div className="grid min-w-0 grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3" role="list">
            <MetricCard label={t("dashboard.businessDate")} testId="kpi-business-date">
              {overviewQuery.data.businessDate}
            </MetricCard>
            <MetricCard
              label={t("dashboard.todaySales")}
              meta={`${overviewQuery.data.todaySaleCount} ${t("dashboard.transactions")}`}
              testId="kpi-today-sales"
            >
              <MoneyDisplay amount={overviewQuery.data.todaySalesTotal} />
            </MetricCard>
            <MetricCard label={t("dashboard.todayCash")} testId="kpi-today-cash">
              <MoneyDisplay amount={overviewQuery.data.todayCashSalesTotal} />
            </MetricCard>
            <MetricCard label={t("dashboard.todayUtang")} testId="kpi-today-utang">
              <MoneyDisplay amount={overviewQuery.data.todayUtangSalesTotal} />
            </MetricCard>
            <MetricCard label={t("dashboard.paymentsReceived")} testId="kpi-payments-received">
              <MoneyDisplay amount={overviewQuery.data.todayPaymentsReceived} />
            </MetricCard>
            <MetricCard label={t("dashboard.openUtang")} testId="kpi-open-utang">
              <MoneyDisplay amount={overviewQuery.data.openUtangOutstanding} />
            </MetricCard>
            <MetricCard label={t("dashboard.lowStock")} testId="kpi-low-stock">
              {overviewQuery.data.lowStockProductCount}
            </MetricCard>
            <MetricCard label={t("dashboard.expiredLots")} testId="kpi-expired-lots">
              {overviewQuery.data.expiredLotCount}
            </MetricCard>
            <MetricCard label={t("dashboard.nearExpiryLots")} testId="kpi-near-expiry">
              {overviewQuery.data.nearExpiryLotCount}
            </MetricCard>
            <MetricCard label={t("dashboard.openShifts")} testId="kpi-open-shifts">
              {overviewQuery.data.openShiftCount}
            </MetricCard>
            <MetricCard label={t("dashboard.activeRegisters")} testId="kpi-active-registers">
              {overviewQuery.data.activeRegisterCount}
            </MetricCard>
          </div>
        ) : null}
      </Card>

      <Card data-testid="period-dashboard-panel">
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-md)] font-semibold">
          {t("dashboard.periodTitle")}
        </h2>
        <p className="mt-0 mb-3 text-[length:var(--exits-text-sm)] text-muted">
          {applied.fromDate} → {applied.toDate}
        </p>
        {dashboardQuery.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {dashboardError ? (
          <ErrorState title={t("reports.errorTitle")} detail={dashboardError} />
        ) : null}
        {dashboardQuery.data ? (
          <>
            <div
              className="grid min-w-0 grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3"
              role="list"
            >
              <MetricCard
                label={t("dashboard.completedSales")}
                meta={`${dashboardQuery.data.completedSaleCount} ${t("dashboard.transactions")}`}
                testId="kpi-period-sales"
              >
                <MoneyDisplay amount={dashboardQuery.data.completedSalesTotal} />
              </MetricCard>
              <MetricCard label={t("dashboard.cashSales")} testId="kpi-period-cash">
                <MoneyDisplay amount={dashboardQuery.data.cashSalesTotal} />
              </MetricCard>
              <MetricCard label={t("dashboard.gcashSales")} testId="kpi-period-gcash">
                <MoneyDisplay amount={dashboardQuery.data.manualGCashSalesTotal} />
              </MetricCard>
              <MetricCard label={t("dashboard.utangSales")} testId="kpi-period-utang">
                <MoneyDisplay amount={dashboardQuery.data.utangSalesTotal} />
              </MetricCard>
              <MetricCard label={t("dashboard.voidedSales")} testId="kpi-period-voids">
                {dashboardQuery.data.voidedSaleCount}
              </MetricCard>
              <MetricCard label={t("dashboard.expenses")} testId="kpi-period-expenses">
                <MoneyDisplay amount={dashboardQuery.data.recordedExpenseTotal} />
              </MetricCard>
            </div>

            <h3 className="mb-2 mt-4 text-[length:var(--exits-text-sm)] font-semibold">
              {t("dashboard.paymentBreakdown")}
            </h3>
            {dashboardQuery.data.paymentMethodBreakdown.length === 0 ? (
              <EmptyState title={t("reports.emptyTitle")} detail={t("reports.emptyDetail")} />
            ) : (
              <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="payment-breakdown">
                {dashboardQuery.data.paymentMethodBreakdown.map((row) => (
                  <li
                    key={row.paymentMethod}
                    className="flex min-w-0 items-center justify-between gap-2 border-b border-border pb-2"
                  >
                    <span>
                      {formatReportPaymentMethod(row.paymentMethod)}
                      <span className="ml-2 text-muted">({row.count})</span>
                    </span>
                    <MoneyDisplay amount={row.amount} />
                  </li>
                ))}
              </ul>
            )}
          </>
        ) : null}
      </Card>
    </div>
  );
}
