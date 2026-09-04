import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { motion, useReducedMotion } from "motion/react";
import { Clock3 } from "lucide-react";
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
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { DashboardComparisonTrend } from "@/features/reports/DashboardMetricCards";
import { AnimatedMoneyValue } from "@/features/reports/dashboard/AnimatedMetricValue";
import {
  DashboardPanel,
  DashboardQuietEmpty,
  DashboardToolbar,
} from "@/features/reports/dashboard/DashboardToolbar";
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

function KpiStripItem({
  label,
  children,
  tone = "default",
  testId,
  metricScope,
}: {
  label: string;
  children: React.ReactNode;
  tone?: "default" | "emphasis" | "attention";
  testId: string;
  metricScope?: "branch" | "organization";
}) {
  return (
    <div
      className={cn(
        "dashboard-kpi-chip",
        tone === "emphasis" && "dashboard-kpi-chip--emphasis",
        tone === "attention" && "dashboard-kpi-chip--attention",
      )}
      data-testid={testId}
      data-metric-scope={metricScope}
      role="listitem"
    >
      <span className="dashboard-kpi-chip__label">{label}</span>
      <span className="dashboard-kpi-chip__value">{children}</span>
    </div>
  );
}

export function ManagementDashboardPage() {
  const { t } = useI18n();
  const reduceMotion = useReducedMotion();
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

  function onCompareBranches() {
    if (allowAll) {
      setScopeSelection({ mode: "all" });
    }
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

  const fadeUp = reduceMotion
    ? undefined
    : { initial: { opacity: 0, y: 8 }, animate: { opacity: 1, y: 0 } };

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div
      className="dashboard-page dashboard-page--v2 exits-page flex min-w-0 flex-col gap-3"
      data-testid="management-dashboard-page"
    >
      <PageHeader
        title={t("dashboard.title")}
        description={t("dashboard.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-reports"
      />

      <DashboardToolbar
        preset={preset}
        range={applied}
        custom={custom}
        onPresetChange={onPresetChange}
        onCustomChange={setCustom}
        onApply={onApply}
        loading={dashboardQuery.isFetching}
        refreshing={refreshing}
        onRefresh={() => {
          void overviewQuery.refetch();
          void dashboardQuery.refetch();
          void productsQuery.refetch();
          void profitabilityQuery.refetch();
          void utangReportQuery.refetch();
          if (branchRankEnabled) {
            void branchRankQuery.refetch();
          }
        }}
        scopeMode={scopeMode}
        organizationId={workspace.organizationId}
        currentBranchId={workspace.branchId}
        currentBranchName={boundWorkspace?.branchName}
        selection={scopeSelection}
        onSelectionChange={setScopeSelection}
        allowAllBranches={allowAll}
      />

      <p className="sr-only" data-testid="dashboard-scope-filter-note">
        {t("dashboard.scope.filterNote")}
      </p>

      {overviewQuery.isLoading && dashboardQuery.isLoading ? (
        <LoadingState label={t("reports.loading")} />
      ) : null}
      {overviewError ? (
        <ErrorState title={t("reports.errorTitle")} detail={overviewError} />
      ) : null}
      {dashboardError ? (
        <ErrorState title={t("reports.errorTitle")} detail={dashboardError} />
      ) : null}

      {dashboardQuery.isLoading && !dashboard ? (
        <div className="dashboard-skeleton" data-testid="dashboard-period-skeleton" aria-busy>
          <div className="dashboard-skeleton__hero" />
          <div className="dashboard-skeleton__chart" />
          <div className="dashboard-skeleton__grid">
            <div className="dashboard-skeleton__panel" />
            <div className="dashboard-skeleton__panel" />
          </div>
        </div>
      ) : null}

      {dashboard ? (
        <motion.div
          className="dashboard-exec dashboard-exec--v2"
          key={animationKey}
          {...(fadeUp ?? {})}
          transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
        >
          <section
            className="dashboard-sales-block"
            data-testid="dashboard-branch-performance"
            data-metric-scope="branch"
          >
            <div className="dashboard-sales-block__header">
              <h2 className="dashboard-section-title">{t("dashboard.section.salesPerformance")}</h2>
              <span className="dashboard-panel__scope" data-testid="scope-period-sales">
                {branchScopeLabel}
              </span>
            </div>

            <article
              className="dashboard-hero"
              data-testid="kpi-period-sales"
              data-metric-scope="branch"
            >
              <span className="dashboard-hero__label">{t("dashboard.totalSales")}</span>
              <div className="dashboard-hero__value">
                <AnimatedMoneyValue
                  amount={dashboard.completedSalesTotal}
                  animationKey={animationKey}
                />
              </div>
              {dashboard.completedSalesTotal <= 0 ? (
                <p className="dashboard-hero__empty m-0">{t("dashboard.noSalesYet")}</p>
              ) : (
                <p className="dashboard-hero__meta m-0">
                  {dashboard.completedSaleCount} {t("dashboard.transactions")}
                </p>
              )}
              {dashboard.salesTotalComparison ? (
                <div className="dashboard-hero__trend">
                  <DashboardComparisonTrend
                    comparison={dashboard.salesTotalComparison}
                    vsPriorLabel={t("dashboard.vsPriorShort")}
                  />
                </div>
              ) : null}
            </article>

            <div className="dashboard-kpi-strip" role="list" data-testid="dashboard-kpi-strip">
              <KpiStripItem
                label={t("dashboard.transactions")}
                testId="kpi-period-txns"
                metricScope="branch"
              >
                {dashboard.completedSaleCount}
              </KpiStripItem>
              <KpiStripItem
                label={t("dashboard.avgSale")}
                testId="kpi-period-avg-sale"
                metricScope="branch"
                tone="emphasis"
              >
                {averageSale != null ? <MoneyDisplay amount={averageSale} /> : "—"}
              </KpiStripItem>
              <KpiStripItem
                label={t("dashboard.cashSales")}
                testId="kpi-period-cash"
                metricScope="branch"
                tone="emphasis"
              >
                <MoneyDisplay amount={dashboard.cashSalesTotal} />
              </KpiStripItem>
              <KpiStripItem
                label={t("dashboard.gcashSales")}
                testId="kpi-period-gcash"
                metricScope="branch"
              >
                <MoneyDisplay amount={dashboard.manualGCashSalesTotal} />
              </KpiStripItem>
              <KpiStripItem
                label={t("dashboard.utangSales")}
                testId="kpi-period-utang"
                metricScope="branch"
              >
                <MoneyDisplay amount={dashboard.utangSalesTotal} />
              </KpiStripItem>
              <KpiStripItem
                label={t("dashboard.voidedSales")}
                testId="kpi-period-voids"
                metricScope="branch"
                tone={dashboard.voidedSaleCount > 0 ? "attention" : "default"}
              >
                {dashboard.voidedSaleCount}
              </KpiStripItem>
              <KpiStripItem
                label={t("dashboard.expenses")}
                testId="kpi-period-expenses"
                metricScope="organization"
              >
                <span data-testid="scope-period-expenses" className="sr-only">
                  {organizationScopeLabel}
                </span>
                <MoneyDisplay amount={dashboard.recordedExpenseTotal} />
              </KpiStripItem>
            </div>

            <DashboardPanel
              title={t("dashboard.salesTrend")}
              className="dashboard-panel--trend"
              testId="dashboard-sales-trend-panel"
            >
              <SalesTrendAreaChart
                points={dashboard.salesByDay}
                emptyTitle={t("dashboard.salesTrendEmpty")}
                emptyDetail={t("dashboard.salesTrendEmptyDetail")}
                animationKey={animationKey}
                ariaLabel={t("dashboard.salesTrend")}
              />
            </DashboardPanel>
          </section>

          <div className="dashboard-exec__analytics">
            <DashboardPanel
              title={t("dashboard.paymentMix")}
              scopeLabel={branchScopeLabel}
              scopeTestId="scope-payment-breakdown"
              testId="dashboard-payment-panel"
            >
              <PaymentMixDonut
                rows={dashboard.paymentMethodBreakdown}
                totalLabel={t("dashboard.totalSales")}
                emptyTitle={t("dashboard.paymentMixEmpty")}
                animationKey={animationKey}
              />
            </DashboardPanel>

            <DashboardPanel
              title={t("dashboard.utangHealth")}
              scopeLabel={organizationScopeLabel}
              scopeTestId="scope-utang-health"
              testId="dashboard-utang-health"
            >
              <UtangOverdueRadial
                outstanding={dashboard.activeCustomerUtangOutstanding}
                overdue={dashboard.overdueUtangAmount}
                overdueLabel={t("dashboard.overdueShare")}
                outstandingLabel={t("dashboard.utangOutstanding")}
                ofLabel={t("dashboard.of")}
                clearTitle={t("dashboard.utangClear")}
                clearDetail={t("dashboard.utangClearDetail")}
                animationKey={animationKey}
                customersHref="/customers"
              />
              {dashboard.activeCustomerUtangOutstanding > 0 ||
              dashboard.utangSalesTotal > 0 ||
              (utangReportQuery.data?.repaymentsRecordedInPeriod ?? 0) > 0 ? (
                <div className="dashboard-mini-facts" role="list">
                  <KpiStripItem label={t("dashboard.utangSales")} testId="kpi-utang-period-sales-fact">
                    <MoneyDisplay amount={dashboard.utangSalesTotal} />
                  </KpiStripItem>
                  {utangReportQuery.data ? (
                    <KpiStripItem label={t("dashboard.repayments")} testId="kpi-utang-repayments">
                      <MoneyDisplay amount={utangReportQuery.data.repaymentsRecordedInPeriod} />
                    </KpiStripItem>
                  ) : null}
                  <KpiStripItem
                    label={t("dashboard.overdueUtang")}
                    testId="kpi-period-overdue-utang"
                    tone={dashboard.overdueUtangAmount > 0 ? "attention" : "default"}
                  >
                    <MoneyDisplay amount={dashboard.overdueUtangAmount} />
                  </KpiStripItem>
                </div>
              ) : null}
            </DashboardPanel>
          </div>

          <div className="dashboard-exec__ops">
            <DashboardPanel
              title={t("dashboard.inventoryHealth")}
              scopeLabel={organizationScopeLabel}
              scopeTestId="scope-inventory-health"
              testId="dashboard-inventory-panel"
            >
              <InventoryHealthBars
                animationKey={animationKey}
                clearTitle={t("dashboard.inventoryClear")}
                clearDetail={t("dashboard.inventoryClearDetail")}
                rows={[
                  {
                    key: "low-stock",
                    label: t("dashboard.lowStock"),
                    count: overview?.lowStockProductCount ?? dashboard.lowStockProductCount,
                    href: "/inventory",
                    tone: "attention",
                  },
                  {
                    key: "near-expiry",
                    label: t("dashboard.nearExpiryLots"),
                    count: overview?.nearExpiryLotCount ?? 0,
                    href: "/inventory/expiration",
                    tone: "attention",
                  },
                  {
                    key: "expired",
                    label: t("dashboard.expiredLots"),
                    count: overview?.expiredLotCount ?? 0,
                    href: "/inventory/expiration",
                    tone: "danger",
                  },
                ]}
              />
            </DashboardPanel>

            {grossProfitAvailable && profitabilityQuery.data ? (
              <DashboardPanel
                title={t("dashboard.grossMargin")}
                scopeLabel={branchScopeLabel}
                scopeTestId="scope-gross-margin"
                testId="dashboard-gross-margin"
              >
                <GrossMarginRadial
                  marginPercent={profitabilityQuery.data.grossMarginPercent!}
                  grossProfit={profitabilityQuery.data.grossProfit!}
                  revenue={profitabilityQuery.data.netSales}
                  marginLabel={t("dashboard.grossMargin")}
                  profitLabel={t("dashboard.grossProfit")}
                  animationKey={animationKey}
                />
              </DashboardPanel>
            ) : null}

            {branchRankEnabled && branchRankQuery.data && branchRankQuery.data.length > 0 ? (
              <DashboardPanel
                title={t("dashboard.branchRanking")}
                scopeLabel={t("dashboard.scope.allBranches")}
                scopeTestId="scope-branch-ranking"
                testId="dashboard-branch-ranking"
              >
                <RankedHorizontalBars
                  rows={branchRankQuery.data}
                  emptyTitle={t("dashboard.branchRankingEmpty")}
                  animationKey={animationKey}
                  testId="dashboard-branch-rank-chart"
                  ariaLabel={t("dashboard.branchRanking")}
                />
              </DashboardPanel>
            ) : (
              <DashboardPanel
                title={t("dashboard.branchRanking")}
                compact
                testId="dashboard-branch-ranking-unavailable"
              >
                <DashboardQuietEmpty
                  title={t("dashboard.branchRankingHint")}
                  testId="dashboard-branch-compare-cta"
                  action={
                    allowAll ? (
                      <button
                        type="button"
                        className="dashboard-toolbar__apply"
                        data-testid="dashboard-compare-branches"
                        onClick={onCompareBranches}
                      >
                        {t("dashboard.compareBranches")}
                      </button>
                    ) : null
                  }
                />
              </DashboardPanel>
            )}
          </div>

          <DashboardPanel
            title={t("dashboard.topProducts")}
            scopeLabel={branchScopeLabel}
            className="dashboard-panel--wide"
            testId="dashboard-top-products-panel"
          >
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
            {productsQuery.isLoading ? (
              <LoadingState label={t("reports.loading")} />
            ) : (
              <RankedHorizontalBars
                rows={topProductRows}
                emptyTitle={t("dashboard.topProductsEmpty")}
                animationKey={`${animationKey}|${productRank}`}
                valueFormatter={
                  productRank === "quantity" ? (v) => v.toLocaleString("en-PH") : formatPeso
                }
                testId="dashboard-top-products-chart"
                emptyTestId="dashboard-top-products-empty"
                ariaLabel={t("dashboard.topProducts")}
              />
            )}
          </DashboardPanel>

          <section
            className="dashboard-ops-strip"
            data-testid="dashboard-organization-overview"
            data-metric-scope="organization"
          >
            <div className="dashboard-sales-block__header">
              <h2 className="dashboard-section-title">{t("dashboard.section.operations")}</h2>
              <span className="dashboard-panel__scope" data-testid="scope-operations">
                {organizationScopeLabel}
              </span>
            </div>
            <div className="dashboard-kpi-strip dashboard-kpi-strip--ops" role="list">
              {overview ? (
                <>
                  <KpiStripItem label={t("dashboard.businessDate")} testId="kpi-business-date">
                    {overview.businessDate}
                  </KpiStripItem>
                  <KpiStripItem
                    label={t("dashboard.openUtang")}
                    testId="kpi-open-utang"
                    tone={overview.openUtangOutstanding > 0 ? "attention" : "default"}
                  >
                    <MoneyDisplay amount={overview.openUtangOutstanding} />
                  </KpiStripItem>
                  <KpiStripItem
                    label={t("dashboard.lowStock")}
                    testId="kpi-low-stock"
                    tone={overview.lowStockProductCount > 0 ? "attention" : "default"}
                  >
                    {overview.lowStockProductCount}
                  </KpiStripItem>
                  <KpiStripItem
                    label={t("dashboard.openShifts")}
                    testId="kpi-open-shifts"
                  >
                    <span className="inline-flex items-center gap-1">
                      <Clock3 className="size-3.5 opacity-60" aria-hidden />
                      {overview.openShiftCount}
                    </span>
                  </KpiStripItem>
                  <KpiStripItem
                    label={t("dashboard.activeRegisters")}
                    testId="kpi-active-registers"
                  >
                    {overview.activeRegisterCount}
                  </KpiStripItem>
                  <KpiStripItem
                    label={t("dashboard.todaySales")}
                    testId="kpi-today-sales"
                    metricScope="organization"
                  >
                    <span data-testid="scope-today-sales" className="sr-only">
                      {organizationScopeLabel}
                    </span>
                    <MoneyDisplay amount={overview.todaySalesTotal} />
                  </KpiStripItem>
                </>
              ) : (
                <KpiStripItem
                  label={t("dashboard.lowStock")}
                  testId="kpi-period-low-stock"
                >
                  {dashboard.lowStockProductCount}
                </KpiStripItem>
              )}
            </div>
          </section>
        </motion.div>
      ) : null}
    </div>
  );
}
