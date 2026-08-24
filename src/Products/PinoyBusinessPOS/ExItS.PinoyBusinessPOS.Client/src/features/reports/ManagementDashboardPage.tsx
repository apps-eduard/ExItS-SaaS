import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { BarChart3, RefreshCw } from "lucide-react";
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
import { ReportFilters } from "@/features/reports/ReportFilters";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

function MetricCard({
  label,
  children,
  meta,
  testId,
  tone = "default",
}: {
  label: string;
  children: React.ReactNode;
  meta?: React.ReactNode;
  testId: string;
  tone?: "default" | "emphasis" | "attention";
}) {
  return (
    <div
      className={cn(
        "dashboard-kpi",
        tone === "emphasis" && "dashboard-kpi--emphasis",
        tone === "attention" && "dashboard-kpi--attention",
      )}
      data-testid={testId}
      role="listitem"
    >
      <span className="dashboard-kpi__label">{label}</span>
      <span className="dashboard-kpi__value">{children}</span>
      {meta ? <span className="dashboard-kpi__meta">{meta}</span> : null}
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
  const refreshing = overviewQuery.isFetching || dashboardQuery.isFetching;
  const overview = overviewQuery.data;

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
          <div className="dashboard-kpi-grid" role="list">
            <MetricCard label={t("dashboard.businessDate")} testId="kpi-business-date">
              {overview.businessDate}
            </MetricCard>
            <MetricCard
              label={t("dashboard.todaySales")}
              meta={`${overview.todaySaleCount} ${t("dashboard.transactions")}`}
              testId="kpi-today-sales"
              tone="emphasis"
            >
              <MoneyDisplay amount={overview.todaySalesTotal} />
            </MetricCard>
            <MetricCard label={t("dashboard.todayCash")} testId="kpi-today-cash">
              <MoneyDisplay amount={overview.todayCashSalesTotal} />
            </MetricCard>
            <MetricCard label={t("dashboard.todayUtang")} testId="kpi-today-utang">
              <MoneyDisplay amount={overview.todayUtangSalesTotal} />
            </MetricCard>
            <MetricCard label={t("dashboard.paymentsReceived")} testId="kpi-payments-received">
              <MoneyDisplay amount={overview.todayPaymentsReceived} />
            </MetricCard>
            <MetricCard label={t("dashboard.openUtang")} testId="kpi-open-utang">
              <MoneyDisplay amount={overview.openUtangOutstanding} />
            </MetricCard>
            <MetricCard
              label={t("dashboard.lowStock")}
              testId="kpi-low-stock"
              tone={overview.lowStockProductCount > 0 ? "attention" : "default"}
            >
              {overview.lowStockProductCount}
            </MetricCard>
            <MetricCard
              label={t("dashboard.expiredLots")}
              testId="kpi-expired-lots"
              tone={overview.expiredLotCount > 0 ? "attention" : "default"}
            >
              {overview.expiredLotCount}
            </MetricCard>
            <MetricCard
              label={t("dashboard.nearExpiryLots")}
              testId="kpi-near-expiry"
              tone={overview.nearExpiryLotCount > 0 ? "attention" : "default"}
            >
              {overview.nearExpiryLotCount}
            </MetricCard>
            <MetricCard label={t("dashboard.openShifts")} testId="kpi-open-shifts">
              {overview.openShiftCount}
            </MetricCard>
            <MetricCard label={t("dashboard.activeRegisters")} testId="kpi-active-registers">
              {overview.activeRegisterCount}
            </MetricCard>
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
        {dashboardQuery.data ? (
          <>
            <div className="dashboard-kpi-grid" role="list">
              <MetricCard
                label={t("dashboard.completedSales")}
                meta={`${dashboardQuery.data.completedSaleCount} ${t("dashboard.transactions")}`}
                testId="kpi-period-sales"
                tone="emphasis"
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

            <div className="dashboard-breakdown">
              <h3 className="catalog-form-section__title">{t("dashboard.paymentBreakdown")}</h3>
              {dashboardQuery.data.paymentMethodBreakdown.length === 0 ? (
                <EmptyState title={t("reports.emptyTitle")} detail={t("reports.emptyDetail")} />
              ) : (
                <ul
                  className="exits-list m-0 grid list-none gap-2 p-0"
                  data-testid="payment-breakdown"
                >
                  {dashboardQuery.data.paymentMethodBreakdown.map((row) => (
                    <li key={row.paymentMethod}>
                      <div className="exits-list__card dashboard-payment-row">
                        <div className="dashboard-payment-row__main min-w-0">
                          <strong className="exits-list__name block truncate font-semibold">
                            {formatReportPaymentMethod(row.paymentMethod)}
                          </strong>
                          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                            {row.count} {t("dashboard.transactions")}
                          </p>
                        </div>
                        <span className="dashboard-payment-row__amount">
                          <MoneyDisplay amount={row.amount} />
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </>
        ) : null}
      </section>
    </div>
  );
}
