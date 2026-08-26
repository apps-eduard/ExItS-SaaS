import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  Banknote,
  BarChart3,
  CalendarDays,
  Clock3,
  Package,
  RefreshCw,
  ShieldAlert,
  Smartphone,
  Wallet,
} from "lucide-react";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  formatReportPaymentMethod,
  getDashboard,
  getManagementOverview,
} from "@/api/pos/pos-reporting-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import {
  DashboardComparisonTrend,
  DashboardHeroMetric,
  DashboardMetricCard,
  DashboardShareRow,
  DashboardSparkBars,
} from "@/features/reports/DashboardMetricCards";
import { ReportFilters } from "@/features/reports/ReportFilters";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function shortDayLabel(date: string): string {
  const parts = date.split("-");
  return parts[2] ?? date;
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

  const overviewError = overviewQuery.isError
    ? describePosApiError(overviewQuery.error, t, "reports.loadError")
    : null;
  const dashboardError = dashboardQuery.isError
    ? describePosApiError(dashboardQuery.error, t, "reports.loadError")
    : null;
  const refreshing = overviewQuery.isFetching || dashboardQuery.isFetching;
  const overview = overviewQuery.data;
  const dashboard = dashboardQuery.data;

  const sparkPoints = useMemo(() => {
    if (!dashboard?.salesByDay.length) {
      return [];
    }
    return dashboard.salesByDay.map((day) => ({
      key: shortDayLabel(day.date),
      value: day.amount,
      title: `${day.date}: ${day.amount.toLocaleString()} (${day.count} txns)`,
    }));
  }, [dashboard?.salesByDay]);

  const paymentTotal = useMemo(() => {
    if (!dashboard) {
      return 0;
    }
    return dashboard.paymentMethodBreakdown.reduce((sum, row) => sum + row.amount, 0);
  }, [dashboard]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div
      className="dashboard-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="management-dashboard-page"
    >
      <PageHeader
        title={t("dashboard.title")}
        description={t("dashboard.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-reports"
      />

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("dashboard.title")}
        testId="dashboard-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "reports",
            label: t("reports.open"),
            icon: <BarChart3 />,
            href: "/reports",
            testId: "open-reports-hub",
          },
          {
            key: "refresh",
            label: t("dashboard.refresh"),
            icon: <RefreshCw />,
            testId: "dashboard-refresh",
            disabled: refreshing,
            onSelect: () => {
              void overviewQuery.refetch();
              void dashboardQuery.refetch();
            },
          },
        ]}
      />

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

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="management-overview-panel"
      >
        <h2 className="catalog-form-section__title">{t("dashboard.todayOverview")}</h2>
        {overviewQuery.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {overviewError ? (
          <ErrorState title={t("reports.errorTitle")} detail={overviewError} />
        ) : null}
        {overview ? (
          <div className="dashboard-metrics" role="list">
            <DashboardHeroMetric
              label={t("dashboard.todaySales")}
              meta={`${overview.todaySaleCount} ${t("dashboard.transactions")} · ${overview.businessDate}`}
              testId="kpi-today-sales"
            >
              <MoneyDisplay amount={overview.todaySalesTotal} />
            </DashboardHeroMetric>

            <div className="dashboard-metric-grid" role="list">
              <DashboardMetricCard
                label={t("dashboard.todayCash")}
                icon={Banknote}
                testId="kpi-today-cash"
                tone="emphasis"
              >
                <MoneyDisplay amount={overview.todayCashSalesTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.todayUtang")}
                icon={Wallet}
                testId="kpi-today-utang"
              >
                <MoneyDisplay amount={overview.todayUtangSalesTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.paymentsReceived")}
                icon={Smartphone}
                testId="kpi-payments-received"
              >
                <MoneyDisplay amount={overview.todayPaymentsReceived} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.openUtang")}
                icon={Wallet}
                testId="kpi-open-utang"
                tone={overview.openUtangOutstanding > 0 ? "attention" : "default"}
              >
                <MoneyDisplay amount={overview.openUtangOutstanding} />
              </DashboardMetricCard>
            </div>

            <div className="dashboard-metric-grid dashboard-metric-grid--ops" role="list">
              <DashboardMetricCard
                label={t("dashboard.businessDate")}
                icon={CalendarDays}
                testId="kpi-business-date"
              >
                {overview.businessDate}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.lowStock")}
                icon={Package}
                testId="kpi-low-stock"
                tone={overview.lowStockProductCount > 0 ? "attention" : "success"}
              >
                {overview.lowStockProductCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.expiredLots")}
                icon={ShieldAlert}
                testId="kpi-expired-lots"
                tone={overview.expiredLotCount > 0 ? "attention" : "default"}
              >
                {overview.expiredLotCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.nearExpiryLots")}
                icon={AlertTriangle}
                testId="kpi-near-expiry"
                tone={overview.nearExpiryLotCount > 0 ? "attention" : "default"}
              >
                {overview.nearExpiryLotCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.openShifts")}
                icon={Clock3}
                testId="kpi-open-shifts"
              >
                {overview.openShiftCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.activeRegisters")}
                icon={BarChart3}
                testId="kpi-active-registers"
              >
                {overview.activeRegisterCount}
              </DashboardMetricCard>
            </div>
          </div>
        ) : null}
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="period-dashboard-panel"
      >
        <div className="dashboard-period-header">
          <h2 className="catalog-form-section__title">{t("dashboard.periodTitle")}</h2>
          <p className="dashboard-period-range m-0 text-[length:var(--exits-text-sm)] text-muted">
            {applied.fromDate} → {applied.toDate}
          </p>
        </div>
        {dashboardQuery.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {dashboardError ? (
          <ErrorState title={t("reports.errorTitle")} detail={dashboardError} />
        ) : null}
        {dashboard ? (
          <div className="dashboard-metrics" role="list">
            <DashboardHeroMetric
              label={t("dashboard.completedSales")}
              meta={`${dashboard.completedSaleCount} ${t("dashboard.transactions")}`}
              testId="kpi-period-sales"
              trend={
                dashboard.salesTotalComparison ? (
                  <DashboardComparisonTrend
                    comparison={dashboard.salesTotalComparison}
                    absoluteLabel={<MoneyDisplay amount={dashboard.salesTotalComparison.absoluteChange ?? 0} />}
                    pctUnavailableLabel={t("dashboard.pctUnavailable")}
                    vsPriorLabel={t("dashboard.vsPriorPeriod")}
                  />
                ) : null
              }
            >
              <MoneyDisplay amount={dashboard.completedSalesTotal} />
            </DashboardHeroMetric>

            <div className="dashboard-chart-panel">
              <h3 className="catalog-form-section__title">{t("dashboard.salesByDay")}</h3>
              <DashboardSparkBars
                points={sparkPoints}
                ariaLabel={t("dashboard.salesByDay")}
                emptyLabel={t("dashboard.salesByDayEmpty")}
              />
            </div>

            <div className="dashboard-metric-grid" role="list">
              <DashboardMetricCard
                label={t("dashboard.cashSales")}
                icon={Banknote}
                testId="kpi-period-cash"
                tone="emphasis"
              >
                <MoneyDisplay amount={dashboard.cashSalesTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.gcashSales")}
                icon={Smartphone}
                testId="kpi-period-gcash"
              >
                <MoneyDisplay amount={dashboard.manualGCashSalesTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.utangSales")}
                icon={Wallet}
                testId="kpi-period-utang"
              >
                <MoneyDisplay amount={dashboard.utangSalesTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.expenses")}
                icon={Wallet}
                testId="kpi-period-expenses"
              >
                <MoneyDisplay amount={dashboard.recordedExpenseTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.utangOutstanding")}
                icon={Wallet}
                testId="kpi-period-utang-outstanding"
                tone={dashboard.activeCustomerUtangOutstanding > 0 ? "attention" : "default"}
              >
                <MoneyDisplay amount={dashboard.activeCustomerUtangOutstanding} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.overdueUtang")}
                icon={AlertTriangle}
                testId="kpi-period-overdue-utang"
                tone={dashboard.overdueUtangAmount > 0 ? "attention" : "default"}
              >
                <MoneyDisplay amount={dashboard.overdueUtangAmount} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.voidedSales")}
                icon={ShieldAlert}
                testId="kpi-period-voids"
                tone={dashboard.voidedSaleCount > 0 ? "attention" : "default"}
              >
                {dashboard.voidedSaleCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.lowStock")}
                icon={Package}
                testId="kpi-period-low-stock"
                tone={dashboard.lowStockProductCount > 0 ? "attention" : "success"}
              >
                {dashboard.lowStockProductCount}
              </DashboardMetricCard>
            </div>

            <div className="dashboard-breakdown">
              <h3 className="catalog-form-section__title">{t("dashboard.paymentBreakdown")}</h3>
              {dashboard.paymentMethodBreakdown.length === 0 ? (
                <EmptyState title={t("reports.emptyTitle")} detail={t("reports.emptyDetail")} />
              ) : (
                <ul className="dashboard-share-list m-0 list-none p-0" data-testid="payment-breakdown">
                  {dashboard.paymentMethodBreakdown.map((row) => (
                    <DashboardShareRow
                      key={row.paymentMethod}
                      testId={`payment-share-${row.paymentMethod}`}
                      label={formatReportPaymentMethod(row.paymentMethod)}
                      meta={`${row.count} ${t("dashboard.transactions")}${
                        paymentTotal > 0
                          ? ` · ${Math.round((row.amount / paymentTotal) * 100)}%`
                          : ""
                      }`}
                      amount={<MoneyDisplay amount={row.amount} />}
                      share={paymentTotal > 0 ? row.amount / paymentTotal : 0}
                    />
                  ))}
                </ul>
              )}
            </div>
          </div>
        ) : null}
      </section>
    </div>
  );
}
