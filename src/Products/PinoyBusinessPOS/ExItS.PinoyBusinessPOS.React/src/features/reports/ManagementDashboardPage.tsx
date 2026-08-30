import { useEffect, useMemo, useState } from "react";
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
import {
  hasOrganizationManagementAuthority,
  isPosOperationsManager,
  isPosOwnerRole,
  resolveEffectivePosRoleCode,
} from "@/access/pos-capabilities";
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
  DashboardScopeBadge,
  DashboardShareRow,
  DashboardSparkBars,
} from "@/features/reports/DashboardMetricCards";
import {
  resolveDashboardBranchDisplayName,
  resolveDashboardBranchScopeLabel,
  resolveDashboardOrganizationScopeLabel,
} from "@/features/reports/dashboard-scope";
import { ReportFilters } from "@/features/reports/ReportFilters";
import { ReportScopeControls } from "@/features/reports/ReportScopeControls";
import {
  canSelectAllBranches,
  listOrganizationBranches,
  reportScopeModeForDashboard,
  resolveReportBranchIdQuery,
  type ReportBranchScopeSelection,
} from "@/features/reports/report-branch-scope";
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

  const scopeMode = reportScopeModeForDashboard();
  const allowAll = canSelectAllBranches({
    hasOrgManagement: hasOrganizationManagementAuthority(sessionGrant),
    isOwner: isPosOwnerRole(sessionGrant),
    isManager: isPosOperationsManager(sessionGrant),
    isReportingUser: resolveEffectivePosRoleCode(sessionGrant)?.toLowerCase() === "reportinguser",
  });

  useEffect(() => {
    setScopeSelection({ mode: "current" });
  }, [workspace?.organizationId]);

  const reportBranchId = resolveReportBranchIdQuery(
    scopeMode,
    scopeSelection,
    workspace?.branchId,
  );

  const branchesQuery = useQuery({
    queryKey: ["dashboard-scope-branches", workspace?.organizationId],
    enabled: Boolean(workspace?.organizationId),
    queryFn: async () => {
      const result = await listOrganizationBranches(workspace!.organizationId);
      if (!result.ok) {
        throw new Error("branches");
      }
      return result.branches.filter((b) => b.status.toLowerCase() !== "inactive");
    },
    staleTime: 60_000,
  });

  const branchDisplayName = useMemo(
    () =>
      resolveDashboardBranchDisplayName(
        scopeSelection,
        workspace?.branchId,
        boundWorkspace?.branchName,
        branchesQuery.data ?? [],
      ),
    [scopeSelection, workspace?.branchId, boundWorkspace?.branchName, branchesQuery.data],
  );

  const branchScopeLabel = useMemo(
    () => resolveDashboardBranchScopeLabel(t, scopeSelection, branchDisplayName),
    [t, scopeSelection, branchDisplayName],
  );

  const organizationScopeLabel = useMemo(
    () => resolveDashboardOrganizationScopeLabel(t),
    [t],
  );

  const overviewQuery = useQuery({
    queryKey: ["management-overview", workspace?.organizationId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getManagementOverview(workspace!, signal),
  });

  const dashboardQuery = useQuery({
    queryKey: [
      "pos-dashboard",
      workspace?.organizationId,
      reportBranchId ?? "all",
      applied.fromDate,
      applied.toDate,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getDashboard(workspace!, applied, signal, reportBranchId),
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
    setApplied(resolveReportDatePreset(preset, new Date(), custom));
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
        scopeSlot={
          workspace ? (
            <div className="flex min-w-0 flex-col gap-1.5">
              <ReportScopeControls
                scopeMode={scopeMode}
                organizationId={workspace.organizationId}
                currentBranchId={workspace.branchId}
                currentBranchName={boundWorkspace?.branchName}
                selection={scopeSelection}
                onSelectionChange={setScopeSelection}
                allowAllBranches={allowAll}
                loading={dashboardQuery.isFetching}
              />
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="dashboard-scope-filter-note"
              >
                {t("dashboard.scope.filterNote")}
              </p>
            </div>
          ) : null
        }
        onPresetChange={onPresetChange}
        onCustomChange={setCustom}
        onApply={onApply}
        loading={dashboardQuery.isFetching}
      />

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="management-overview-panel"
      >
        <div className="dashboard-section__header">
          <h2 className="catalog-form-section__title">{t("dashboard.section.organizationOverview")}</h2>
          <p className="dashboard-section__lede">{t("dashboard.todayOverview")}</p>
        </div>
        {overviewQuery.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
        {overviewError ? (
          <ErrorState title={t("reports.errorTitle")} detail={overviewError} />
        ) : null}
        {overview ? (
          <div className="dashboard-metrics" role="list">
            <DashboardHeroMetric
              label={t("dashboard.todaySales")}
              meta={`${overview.todaySaleCount} ${t("dashboard.transactions")} · ${overview.businessDate}`}
              scopeLabel={organizationScopeLabel}
              scopeTestId="scope-today-sales"
              metricScope="organization"
              testId="kpi-today-sales"
            >
              <MoneyDisplay amount={overview.todaySalesTotal} />
            </DashboardHeroMetric>

            <div className="dashboard-metric-grid" role="list">
              <DashboardMetricCard
                label={t("dashboard.todayCash")}
                icon={Banknote}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-today-cash"
                metricScope="organization"
                testId="kpi-today-cash"
                tone="emphasis"
              >
                <MoneyDisplay amount={overview.todayCashSalesTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.todayUtang")}
                icon={Wallet}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-today-utang"
                metricScope="organization"
                testId="kpi-today-utang"
              >
                <MoneyDisplay amount={overview.todayUtangSalesTotal} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.paymentsReceived")}
                icon={Smartphone}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-payments-received"
                metricScope="organization"
                testId="kpi-payments-received"
              >
                <MoneyDisplay amount={overview.todayPaymentsReceived} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.openUtang")}
                icon={Wallet}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-open-utang"
                metricScope="organization"
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
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-business-date"
                metricScope="organization"
                testId="kpi-business-date"
              >
                {overview.businessDate}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.lowStock")}
                icon={Package}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-low-stock-today"
                metricScope="organization"
                testId="kpi-low-stock"
                tone={overview.lowStockProductCount > 0 ? "attention" : "success"}
              >
                {overview.lowStockProductCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.expiredLots")}
                icon={ShieldAlert}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-expired-lots"
                metricScope="organization"
                testId="kpi-expired-lots"
                tone={overview.expiredLotCount > 0 ? "attention" : "default"}
              >
                {overview.expiredLotCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.nearExpiryLots")}
                icon={AlertTriangle}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-near-expiry"
                metricScope="organization"
                testId="kpi-near-expiry"
                tone={overview.nearExpiryLotCount > 0 ? "attention" : "default"}
              >
                {overview.nearExpiryLotCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.openShifts")}
                icon={Clock3}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-open-shifts"
                metricScope="organization"
                testId="kpi-open-shifts"
              >
                {overview.openShiftCount}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("dashboard.activeRegisters")}
                icon={BarChart3}
                scopeLabel={organizationScopeLabel}
                scopeTestId="scope-active-registers"
                metricScope="organization"
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
            <section
              className="dashboard-section"
              data-testid="dashboard-branch-performance"
            >
              <div className="dashboard-section__header">
                <h3 className="catalog-form-section__title">{t("dashboard.section.branchPerformance")}</h3>
              </div>

              <DashboardHeroMetric
                label={t("dashboard.completedSales")}
                meta={`${dashboard.completedSaleCount} ${t("dashboard.transactions")}`}
                scopeLabel={branchScopeLabel}
                scopeTestId="scope-period-sales"
                metricScope="branch"
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
                <div className="dashboard-section__header dashboard-section__header--inline">
                  <h4 className="catalog-form-section__title">{t("dashboard.salesByDay")}</h4>
                  <DashboardScopeBadge label={branchScopeLabel} testId="scope-sales-by-day" />
                </div>
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
                  scopeLabel={branchScopeLabel}
                  scopeTestId="scope-period-cash"
                  metricScope="branch"
                  testId="kpi-period-cash"
                  tone="emphasis"
                >
                  <MoneyDisplay amount={dashboard.cashSalesTotal} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.gcashSales")}
                  icon={Smartphone}
                  scopeLabel={branchScopeLabel}
                  scopeTestId="scope-period-gcash"
                  metricScope="branch"
                  testId="kpi-period-gcash"
                >
                  <MoneyDisplay amount={dashboard.manualGCashSalesTotal} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.utangSales")}
                  icon={Wallet}
                  scopeLabel={branchScopeLabel}
                  scopeTestId="scope-period-utang-sales"
                  metricScope="branch"
                  testId="kpi-period-utang"
                >
                  <MoneyDisplay amount={dashboard.utangSalesTotal} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.voidedSales")}
                  icon={ShieldAlert}
                  scopeLabel={branchScopeLabel}
                  scopeTestId="scope-period-voids"
                  metricScope="branch"
                  testId="kpi-period-voids"
                  tone={dashboard.voidedSaleCount > 0 ? "attention" : "default"}
                >
                  {dashboard.voidedSaleCount}
                </DashboardMetricCard>
              </div>

              <div className="dashboard-breakdown">
                <div className="dashboard-section__header dashboard-section__header--inline">
                  <h4 className="catalog-form-section__title">{t("dashboard.paymentBreakdown")}</h4>
                  <DashboardScopeBadge label={branchScopeLabel} testId="scope-payment-breakdown" />
                </div>
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
            </section>

            <section
              className="dashboard-section"
              data-testid="dashboard-organization-overview"
            >
              <div className="dashboard-section__header">
                <h3 className="catalog-form-section__title">{t("dashboard.section.organizationOverview")}</h3>
                <p className="dashboard-section__lede">{t("dashboard.scope.periodOrgNote")}</p>
              </div>

              <div className="dashboard-metric-grid" role="list">
                <DashboardMetricCard
                  label={t("dashboard.expenses")}
                  icon={Wallet}
                  scopeLabel={organizationScopeLabel}
                  scopeTestId="scope-period-expenses"
                  metricScope="organization"
                  testId="kpi-period-expenses"
                >
                  <MoneyDisplay amount={dashboard.recordedExpenseTotal} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.utangOutstanding")}
                  icon={Wallet}
                  scopeLabel={organizationScopeLabel}
                  scopeTestId="scope-period-utang-outstanding"
                  metricScope="organization"
                  testId="kpi-period-utang-outstanding"
                  tone={dashboard.activeCustomerUtangOutstanding > 0 ? "attention" : "default"}
                >
                  <MoneyDisplay amount={dashboard.activeCustomerUtangOutstanding} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.overdueUtang")}
                  icon={AlertTriangle}
                  scopeLabel={organizationScopeLabel}
                  scopeTestId="scope-period-overdue-utang"
                  metricScope="organization"
                  testId="kpi-period-overdue-utang"
                  tone={dashboard.overdueUtangAmount > 0 ? "attention" : "default"}
                >
                  <MoneyDisplay amount={dashboard.overdueUtangAmount} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.lowStock")}
                  icon={Package}
                  scopeLabel={organizationScopeLabel}
                  scopeTestId="scope-period-low-stock"
                  metricScope="organization"
                  testId="kpi-period-low-stock"
                  tone={dashboard.lowStockProductCount > 0 ? "attention" : "success"}
                >
                  {dashboard.lowStockProductCount}
                </DashboardMetricCard>
              </div>
            </section>
          </div>
        ) : null}
      </section>
    </div>
  );
}
