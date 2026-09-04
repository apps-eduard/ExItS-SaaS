import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeftRight, LayoutDashboard, BarChart3 } from "lucide-react";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  canAccessReportsHub,
  canViewDashboard,
} from "@/access/pos-capabilities";
import { getManagementOverview } from "@/api/pos/pos-reporting-client";
import { buildAdminNavGroups } from "@/features/admin/admin-nav-config";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  DashboardHeroMetric,
  DashboardMetricCard,
} from "@/features/reports/DashboardMetricCards";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Manage Business overview (`/org`) — management summary + admin IA shortcuts.
 * Desktop: compact cards/lists. Mobile: tile-friendly hub sections.
 */
export function OrgEssentialsPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canDashboard = canViewDashboard(sessionGrant);
  const canReports = canAccessReportsHub(sessionGrant);
  const navGroups = buildAdminNavGroups(sessionGrant);

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

  const overviewQuery = useQuery({
    queryKey: ["org-home-overview", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace) && canDashboard && online,
    queryFn: ({ signal }) => getManagementOverview(workspace!, signal),
  });

  const overview = overviewQuery.data;
  const overviewError = overviewQuery.isError
    ? describePosApiError(overviewQuery.error, t, "reports.loadError")
    : null;

  const organizationGroup = navGroups.find((g) => g.id === "organization");
  const businessGroup = navGroups.find((g) => g.id === "business");
  const securityGroup = navGroups.find((g) => g.id === "security");

  return (
    <div
      className="admin-overview-page exits-page mx-auto flex w-full max-w-[1200px] min-w-0 flex-col gap-4"
      data-testid="org-essentials-page"
    >
      <PageHeader
        title={t("admin.shell.manageBusiness")}
        description={t("org.lede")}
        subtitle={boundWorkspace?.organizationDisplayName}
      />

      {canDashboard ? (
        <section
          className="catalog-form-section exits-animate-panel gap-3"
          data-testid="org-group-today"
        >
          <h2 className="catalog-form-section__title text-muted">{t("org.group.today")}</h2>
          {!online ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="org-overview-offline">
              {t("org.overviewOffline")}
            </p>
          ) : null}
          {online && overviewQuery.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
          {online && overviewError ? (
            <ErrorState title={t("reports.errorTitle")} detail={overviewError} />
          ) : null}
          {online && overview ? (
            <div className="admin-overview-metrics dashboard-metrics" data-testid="org-admin-overview">
              <DashboardHeroMetric
                label={t("dashboard.todaySales")}
                meta={`${overview.todaySaleCount} ${t("dashboard.transactions")}`}
                testId="org-kpi-today-sales"
              >
                <MoneyDisplay amount={overview.todaySalesTotal} />
              </DashboardHeroMetric>
              <div className="dashboard-metric-grid" role="list">
                <DashboardMetricCard label={t("dashboard.openShifts")} testId="org-kpi-open-shifts">
                  {overview.openShiftCount}
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.lowStock")}
                  testId="org-kpi-low-stock"
                  tone={overview.lowStockProductCount > 0 ? "attention" : "success"}
                >
                  {overview.lowStockProductCount}
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={t("dashboard.openUtang")}
                  testId="org-kpi-open-utang"
                  tone={overview.openUtangOutstanding > 0 ? "attention" : "default"}
                >
                  <MoneyDisplay amount={overview.openUtangOutstanding} />
                </DashboardMetricCard>
              </div>
            </div>
          ) : null}
          <div className="flex flex-wrap gap-2">
            {canDashboard ? (
              <Link className="admin-overview-link" to="/dashboard" data-testid="open-org-dashboard">
                <LayoutDashboard className="size-4" aria-hidden />
                {t("dashboard.open")}
              </Link>
            ) : null}
            {canReports ? (
              <Link className="admin-overview-link" to="/reports" data-testid="open-org-reports">
                <BarChart3 className="size-4" aria-hidden />
                {t("reports.open")}
              </Link>
            ) : null}
          </div>
        </section>
      ) : null}

      {organizationGroup ? (
        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="admin-overview-organization">
          <h2 className="catalog-form-section__title text-muted">{t(organizationGroup.titleKey)}</h2>
          <div className="admin-hub-grid admin-hub-grid--overview">
            {organizationGroup.items.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.id}
                  to={item.to}
                  className="admin-hub-tile"
                  data-testid={`overview-${item.testId}`}
                >
                  <Icon className="size-5 shrink-0" aria-hidden />
                  <span className="min-w-0">
                    <span className="block font-semibold">{t(item.labelKey)}</span>
                    {item.locked && item.lockedReasonKey ? (
                      <span className="block text-[length:var(--exits-text-xs)] text-muted">
                        {t(item.lockedReasonKey)}
                      </span>
                    ) : null}
                  </span>
                </Link>
              );
            })}
          </div>
        </section>
      ) : null}

      {businessGroup ? (
        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="admin-overview-business">
          <h2 className="catalog-form-section__title text-muted">{t(businessGroup.titleKey)}</h2>
          <div className="admin-hub-grid admin-hub-grid--overview">
            {businessGroup.items.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.id}
                  to={item.to}
                  className="admin-hub-tile"
                  data-testid={`overview-${item.testId}`}
                >
                  <Icon className="size-5 shrink-0" aria-hidden />
                  <span className="font-semibold">{t(item.labelKey)}</span>
                </Link>
              );
            })}
          </div>
        </section>
      ) : null}

      {securityGroup ? (
        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="admin-overview-security">
          <h2 className="catalog-form-section__title text-muted">{t(securityGroup.titleKey)}</h2>
          <div className="admin-hub-grid admin-hub-grid--overview">
            {securityGroup.items.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.id}
                  to={item.to}
                  className="admin-hub-tile"
                  data-testid={`overview-${item.testId}`}
                >
                  <Icon className="size-5 shrink-0" aria-hidden />
                  <span className="font-semibold">{t(item.labelKey)}</span>
                </Link>
              );
            })}
          </div>
        </section>
      ) : null}

      <p className="m-0 text-[length:var(--exits-text-sm)]">
        <Link to="/workspace" className="inline-flex items-center gap-1.5" data-testid="open-switch-workspace">
          <ArrowLeftRight className="size-4" aria-hidden />
          {t("workspace.switch")}
        </Link>
      </p>
    </div>
  );
}
