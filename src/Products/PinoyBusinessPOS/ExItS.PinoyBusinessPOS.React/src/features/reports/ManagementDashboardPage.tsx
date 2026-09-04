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
  getDashboard,
  getManagementOverview,
  getProfitabilityReport,
  getSalesByProductReport,
  getUtangReport,
} from "@/api/pos/pos-reporting-client";
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
} from "@/features/reports/DashboardMetricCards";
import { AnimatedMoneyValue } from "@/features/reports/dashboard/AnimatedMetricValue";
import { InventoryHealthBars } from "@/features/reports/dashboard/InventoryHealthBars";
import { PaymentMixDonut } from "@/features/reports/dashboard/PaymentMixDonut";
import { RankedHorizontalBars } from "@/features/reports/dashboard/RankedHorizontalBars";
import { GrossMarginRadial, UtangOverdueRadial } from "@/features/reports/dashboard/RadialKpis";
import { SalesTrendAreaChart } from "@/features/reports/dashboard/SalesTrendAreaChart";
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
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const BRANCH_RANK_LIMIT = 8;

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
  const [productRank, setProductRank] = useState<"sales" | "quantity">("sales");

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

  const animationKey = `${reportBranchId ?? "all"}|${applied.fromDate}|${applied.toDate}`;

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

  const productsQuery = useQuery({
    queryKey: [
      "dashboard-sales-by-product",
      workspace?.organizationId,
      reportBranchId ?? "all",
      applied.fromDate,
      applied.toDate,
    ],
    enabled: Boolean(workspace) && dashboardQuery.isSuccess,
    queryFn: ({ signal }) => getSalesByProductReport(workspace!, applied, signal, reportBranchId),
    staleTime: 30_000,
  });

  const profitabilityQuery = useQuery({
    queryKey: [
      "dashboard-profitability",
      workspace?.organizationId,
      reportBranchId ?? "all",
      applied.fromDate,
      applied.toDate,
    ],
    enabled: Boolean(workspace) && dashboardQuery.isSuccess,
    queryFn: ({ signal }) => getProfitabilityReport(workspace!, applied, signal, reportBranchId),
    staleTime: 30_000,
  });

  const utangReportQuery = useQuery({
    queryKey: ["dashboard-utang-report", workspace?.organizationId, applied.fromDate, applied.toDate],
    enabled: Boolean(workspace) && dashboardQuery.isSuccess,
    queryFn: ({ signal }) => getUtangReport(workspace!, applied, signal),
    staleTime: 30_000,
  });

  const branchRankEnabled =
    Boolean(workspace) &&
    allowAll &&
    scopeSelection.mode === "all" &&
    (branchesQuery.data?.length ?? 0) >= 2 &&
    (branchesQuery.data?.length ?? 0) <= BRANCH_RANK_LIMIT;

  const branchRankQuery = useQuery({
    queryKey: [
      "dashboard-branch-rank",
      workspace?.organizationId,
      applied.fromDate,
      applied.toDate,
      (branchesQuery.data ?? []).map((b) => b.id).join(","),
    ],
    enabled: branchRankEnabled,
    queryFn: async ({ signal }) => {
      const branches = branchesQuery.data ?? [];
      const rows = await Promise.all(
        branches.map(async (branch) => {
          const dash = await getDashboard(workspace!, applied, signal, branch.id);
          return {
            id: branch.id,
            name: branch.name,
            value: dash.completedSalesTotal,
          };
        }),
      );
      return rows.sort((a, b) => b.value - a.value);
    },
    staleTime: 30_000,
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
  const refreshing =
    overviewQuery.isFetching ||
    dashboardQuery.isFetching ||
    productsQuery.isFetching ||
    profitabilityQuery.isFetching ||
    utangReportQuery.isFetching ||
    branchRankQuery.isFetching;
  const overview = overviewQuery.data;
  const dashboard = dashboardQuery.data;

  const averageSale =
    dashboard && dashboard.completedSaleCount > 0
      ? dashboard.completedSalesTotal / dashboard.completedSaleCount
      : null;

  const grossProfitAvailable =
    profitabilityQuery.data?.cogsStatus === "Complete" &&
    profitabilityQuery.data.grossProfit != null &&
    profitabilityQuery.data.grossMarginPercent != null &&
    profitabilityQuery.data.totalCogs != null;

  const topProductRows = useMemo(() => {
    const rows = productsQuery.data?.rows ?? [];
    const sorted = [...rows].sort((a, b) =>
      productRank === "quantity"
        ? b.netQuantity - a.netQuantity
        : b.netAmount - a.netAmount,
    );
    return sorted.slice(0, 8).map((row) => ({
      id: row.productId,
      name: row.productName,
      value: productRank === "quantity" ? row.netQuantity : row.netAmount,
      display:
        productRank === "quantity"
          ? row.netQuantity.toLocaleString("en-PH")
          : formatPeso(row.netAmount),
    }));
  }, [productsQuery.data?.rows, productRank]);

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
            icon: <RefreshCw className={cn(refreshing && "dashboard-refresh-spin")} />,
            testId: "dashboard-refresh",
            disabled: refreshing,
            onSelect: () => {
              void overviewQuery.refetch();
              void dashboardQuery.refetch();
              void productsQuery.refetch();
              void profitabilityQuery.refetch();
              void utangReportQuery.refetch();
              if (branchRankEnabled) {
                void branchRankQuery.refetch();
              }
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
              <AnimatedMoneyValue
                amount={overview.todaySalesTotal}
                animationKey={`today|${overview.businessDate}`}
              />
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
                to="/customers"
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
                to="/customers"
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
                to="/inventory"
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
                to="/inventory/expiration"
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
                to="/inventory/expiration"
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
        {dashboardQuery.isLoading ? (
          <div className="dashboard-skeleton" data-testid="dashboard-period-skeleton" aria-busy>
            <div className="dashboard-skeleton__hero" />
            <div className="dashboard-skeleton__chart" />
            <div className="dashboard-skeleton__grid">
              <div className="dashboard-skeleton__panel" />
              <div className="dashboard-skeleton__panel" />
            </div>
          </div>
        ) : null}
        {dashboardError ? (
          <ErrorState title={t("reports.errorTitle")} detail={dashboardError} />
        ) : null}
        {dashboard ? (
          <div className="dashboard-metrics dashboard-exec" role="list">
            <section
              className="dashboard-section dashboard-exec__sales"
              data-testid="dashboard-branch-performance"
            >
              <div className="dashboard-section__header">
                <h3 className="catalog-form-section__title">{t("dashboard.section.salesPerformance")}</h3>
              </div>

              <DashboardHeroMetric
                label={t("dashboard.completedSales")}
                meta={`${dashboard.completedSaleCount} ${t("dashboard.transactions")}`}
                scopeLabel={branchScopeLabel}
                scopeTestId="scope-period-sales"
                metricScope="branch"
                testId="kpi-period-sales"
                className="dashboard-hero-metric--flagship"
                trend={
                  dashboard.salesTotalComparison ? (
                    <DashboardComparisonTrend
                      comparison={dashboard.salesTotalComparison}
                      absoluteLabel={
                        <MoneyDisplay amount={dashboard.salesTotalComparison.absoluteChange ?? 0} />
                      }
                      pctUnavailableLabel={t("dashboard.pctUnavailable")}
                      vsPriorLabel={t("dashboard.vsPriorPeriod")}
                    />
                  ) : null
                }
              >
                <AnimatedMoneyValue
                  amount={dashboard.completedSalesTotal}
                  animationKey={animationKey}
                />
              </DashboardHeroMetric>

              <div className="dashboard-metric-grid dashboard-metric-grid--secondary" role="list">
                <DashboardMetricCard
                  label={t("dashboard.transactions")}
                  icon={BarChart3}
                  scopeLabel={branchScopeLabel}
                  metricScope="branch"
                  testId="kpi-period-txns"
                >
                  {dashboard.completedSaleCount}
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.avgSale")}
                  icon={Banknote}
                  scopeLabel={branchScopeLabel}
                  metricScope="branch"
                  testId="kpi-period-avg-sale"
                  tone="emphasis"
                >
                  {averageSale != null ? (
                    <MoneyDisplay amount={averageSale} />
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </DashboardMetricCard>
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
                  to="/customers"
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

              <div className="dashboard-chart-panel dashboard-chart-panel--wide">
                <div className="dashboard-section__header dashboard-section__header--inline">
                  <h4 className="catalog-form-section__title">{t("dashboard.salesTrend")}</h4>
                  <DashboardScopeBadge label={branchScopeLabel} testId="scope-sales-by-day" />
                </div>
                <SalesTrendAreaChart
                  points={dashboard.salesByDay}
                  emptyTitle={t("reports.emptyTitle")}
                  emptyDetail={t("dashboard.salesByDayEmpty")}
                  animationKey={animationKey}
                  ariaLabel={t("dashboard.salesTrend")}
                />
              </div>
            </section>

            <div className="dashboard-exec__analytics">
              <div className="dashboard-chart-panel">
                <div className="dashboard-section__header dashboard-section__header--inline">
                  <h4 className="catalog-form-section__title">{t("dashboard.paymentMix")}</h4>
                  <DashboardScopeBadge label={branchScopeLabel} testId="scope-payment-breakdown" />
                </div>
                <PaymentMixDonut
                  rows={dashboard.paymentMethodBreakdown}
                  totalLabel={t("dashboard.completedSales")}
                  emptyTitle={t("reports.emptyTitle")}
                  emptyDetail={t("reports.emptyDetail")}
                  animationKey={animationKey}
                />
              </div>

              <div className="dashboard-chart-panel" data-testid="dashboard-utang-health">
                <div className="dashboard-section__header dashboard-section__header--inline">
                  <h4 className="catalog-form-section__title">{t("dashboard.utangHealth")}</h4>
                  <DashboardScopeBadge
                    label={organizationScopeLabel}
                    testId="scope-utang-health"
                  />
                </div>
                <UtangOverdueRadial
                  outstanding={dashboard.activeCustomerUtangOutstanding}
                  overdue={dashboard.overdueUtangAmount}
                  overdueLabel={t("dashboard.overdueShare")}
                  outstandingLabel={t("dashboard.utangOutstanding")}
                  ofLabel={t("dashboard.of")}
                  animationKey={animationKey}
                  customersHref="/customers"
                />
                <div className="dashboard-utang-facts" role="list">
                  <DashboardMetricCard
                    label={t("dashboard.utangSales")}
                    icon={Wallet}
                    metricScope="branch"
                    testId="kpi-utang-period-sales-fact"
                  >
                    <MoneyDisplay amount={dashboard.utangSalesTotal} />
                  </DashboardMetricCard>
                  {utangReportQuery.data ? (
                    <DashboardMetricCard
                      label={t("dashboard.repayments")}
                      icon={Banknote}
                      metricScope="organization"
                      testId="kpi-utang-repayments"
                      to="/customers"
                    >
                      <MoneyDisplay amount={utangReportQuery.data.repaymentsRecordedInPeriod} />
                    </DashboardMetricCard>
                  ) : null}
                  <DashboardMetricCard
                    label={t("dashboard.overdueUtang")}
                    icon={AlertTriangle}
                    scopeLabel={organizationScopeLabel}
                    scopeTestId="scope-period-overdue-utang"
                    metricScope="organization"
                    testId="kpi-period-overdue-utang"
                    tone={dashboard.overdueUtangAmount > 0 ? "attention" : "default"}
                    to="/customers"
                  >
                    <MoneyDisplay amount={dashboard.overdueUtangAmount} />
                  </DashboardMetricCard>
                </div>
              </div>
            </div>

            <div className="dashboard-exec__ops">
              <div className="dashboard-chart-panel">
                <div className="dashboard-section__header dashboard-section__header--inline">
                  <h4 className="catalog-form-section__title">{t("dashboard.inventoryHealth")}</h4>
                  <DashboardScopeBadge
                    label={organizationScopeLabel}
                    testId="scope-inventory-health"
                  />
                </div>
                <InventoryHealthBars
                  animationKey={animationKey}
                  rows={[
                    {
                      key: "low-stock",
                      label: t("dashboard.lowStock"),
                      count: overview?.lowStockProductCount ?? dashboard.lowStockProductCount,
                      href: "/inventory",
                      tone:
                        (overview?.lowStockProductCount ?? dashboard.lowStockProductCount) > 0
                          ? "attention"
                          : "default",
                    },
                    {
                      key: "near-expiry",
                      label: t("dashboard.nearExpiryLots"),
                      count: overview?.nearExpiryLotCount ?? 0,
                      href: "/inventory/expiration",
                      tone: (overview?.nearExpiryLotCount ?? 0) > 0 ? "attention" : "default",
                    },
                    {
                      key: "expired",
                      label: t("dashboard.expiredLots"),
                      count: overview?.expiredLotCount ?? 0,
                      href: "/inventory/expiration",
                      tone: (overview?.expiredLotCount ?? 0) > 0 ? "danger" : "default",
                    },
                  ]}
                />
              </div>

              {grossProfitAvailable && profitabilityQuery.data ? (
                <div className="dashboard-chart-panel" data-testid="dashboard-gross-margin">
                  <div className="dashboard-section__header dashboard-section__header--inline">
                    <h4 className="catalog-form-section__title">{t("dashboard.grossMargin")}</h4>
                    <DashboardScopeBadge label={branchScopeLabel} testId="scope-gross-margin" />
                  </div>
                  <GrossMarginRadial
                    marginPercent={profitabilityQuery.data.grossMarginPercent!}
                    grossProfit={profitabilityQuery.data.grossProfit!}
                    revenue={profitabilityQuery.data.netSales}
                    marginLabel={t("dashboard.grossMargin")}
                    profitLabel={t("dashboard.grossProfit")}
                    animationKey={animationKey}
                  />
                </div>
              ) : null}

              {branchRankEnabled && branchRankQuery.data && branchRankQuery.data.length > 0 ? (
                <div className="dashboard-chart-panel" data-testid="dashboard-branch-ranking">
                  <div className="dashboard-section__header dashboard-section__header--inline">
                    <h4 className="catalog-form-section__title">{t("dashboard.branchRanking")}</h4>
                    <DashboardScopeBadge
                      label={t("dashboard.scope.allBranches")}
                      testId="scope-branch-ranking"
                    />
                  </div>
                  <RankedHorizontalBars
                    rows={branchRankQuery.data}
                    emptyTitle={t("reports.emptyTitle")}
                    emptyDetail={t("dashboard.branchRankingEmpty")}
                    animationKey={animationKey}
                    testId="dashboard-branch-rank-chart"
                    ariaLabel={t("dashboard.branchRanking")}
                  />
                </div>
              ) : !grossProfitAvailable ? (
                <div
                  className="dashboard-chart-panel dashboard-chart-panel--muted"
                  data-testid="dashboard-branch-ranking-unavailable"
                >
                  <div className="dashboard-section__header">
                    <h4 className="catalog-form-section__title">{t("dashboard.branchRanking")}</h4>
                    <p className="dashboard-section__lede m-0">
                      {t("dashboard.branchRankingHint")}
                    </p>
                  </div>
                </div>
              ) : null}
            </div>

            <div className="dashboard-chart-panel dashboard-chart-panel--wide">
              <div className="dashboard-section__header dashboard-section__header--inline">
                <h4 className="catalog-form-section__title">{t("dashboard.topProducts")}</h4>
                <div className="dashboard-rank-toggle" role="group" aria-label={t("dashboard.topProducts")}>
                  <button
                    type="button"
                    className={cn(
                      "dashboard-rank-toggle__btn",
                      productRank === "sales" && "dashboard-rank-toggle__btn--active",
                    )}
                    onClick={() => setProductRank("sales")}
                    data-testid="top-products-rank-sales"
                  >
                    {t("dashboard.rankBySales")}
                  </button>
                  <button
                    type="button"
                    className={cn(
                      "dashboard-rank-toggle__btn",
                      productRank === "quantity" && "dashboard-rank-toggle__btn--active",
                    )}
                    onClick={() => setProductRank("quantity")}
                    data-testid="top-products-rank-qty"
                  >
                    {t("dashboard.rankByQuantity")}
                  </button>
                </div>
              </div>
              {productsQuery.isLoading ? (
                <LoadingState label={t("reports.loading")} />
              ) : (
                <RankedHorizontalBars
                  rows={topProductRows}
                  emptyTitle={t("reports.emptyTitle")}
                  emptyDetail={t("dashboard.topProductsEmpty")}
                  animationKey={`${animationKey}|${productRank}`}
                  valueFormatter={
                    productRank === "quantity"
                      ? (v) => v.toLocaleString("en-PH")
                      : formatPeso
                  }
                  testId="dashboard-top-products-chart"
                  ariaLabel={t("dashboard.topProducts")}
                />
              )}
            </div>

            <section
              className="dashboard-section"
              data-testid="dashboard-organization-overview"
            >
              <div className="dashboard-section__header">
                <h3 className="catalog-form-section__title">
                  {t("dashboard.section.organizationOverview")}
                </h3>
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
                  to="/customers"
                >
                  <MoneyDisplay amount={dashboard.activeCustomerUtangOutstanding} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.overdueUtang")}
                  icon={AlertTriangle}
                  scopeLabel={organizationScopeLabel}
                  metricScope="organization"
                  testId="kpi-period-overdue-utang-org"
                  tone={dashboard.overdueUtangAmount > 0 ? "attention" : "default"}
                  to="/customers"
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
                  to="/inventory"
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
