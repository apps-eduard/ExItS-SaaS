import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeftRight,
  Banknote,
  BarChart3,
  KeyRound,
  LayoutDashboard,
  MapPin,
  MonitorSmartphone,
  Package,
  QrCode,
  ShieldCheck,
  ShoppingCart,
  Users,
} from "lucide-react";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  canAccessReportsHub,
  canCreateSale,
  canInviteOrganizationStaff,
  canManageCatalog,
  canViewDashboard,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { getManagementOverview } from "@/api/pos/pos-reporting-client";
import { ActionTileGrid, type ActionTileDef } from "@/components/exits/ActionTileGrid";
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
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function GuideSection({
  title,
  children,
  testId,
}: {
  title: string;
  children: React.ReactNode;
  testId?: string;
}) {
  return (
    <section
      className="catalog-form-section exits-animate-panel manager-home-section gap-3"
      data-testid={testId}
    >
      <h2 className="catalog-form-section__title text-muted">{title}</h2>
      {children}
    </section>
  );
}

/**
 * Manage business home (`/org`) — Owner / OrganizationAdministrator only.
 * Shows real today overview (management/overview API) plus admin menus.
 * Does not invent KPIs; full analytics stay on `/dashboard` and `/reports`.
 */
export function OrgEssentialsPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canInvite = canInviteOrganizationStaff(sessionGrant);
  const canSell = canCreateSale(sessionGrant);
  const canCatalog = canManageCatalog(sessionGrant);
  const canAdmin = hasOrganizationManagementAuthority(sessionGrant);
  const canDashboard = canViewDashboard(sessionGrant);
  const canReports = canAccessReportsHub(sessionGrant);

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

  const insightTiles: ActionTileDef[] = [];
  if (canDashboard) {
    insightTiles.push({
      key: "dashboard",
      label: t("dashboard.open"),
      icon: LayoutDashboard,
      testId: "open-org-dashboard",
      to: "/dashboard",
      primary: true,
    });
  }
  if (canReports) {
    insightTiles.push({
      key: "reports",
      label: t("reports.open"),
      icon: BarChart3,
      testId: "open-org-reports",
      to: "/reports",
    });
  }

  const administrationTiles: ActionTileDef[] = [];
  if (canInvite) {
    administrationTiles.push(
      {
        key: "staff",
        label: t("staffManage.title"),
        icon: Users,
        testId: "open-staff-manage",
        to: "/org/staff",
      },
      {
        key: "roles",
        label: t("orgRoles.title"),
        icon: ShieldCheck,
        testId: "open-org-roles",
        to: "/org/roles",
      },
      {
        key: "branches",
        label: t("org.branchesLink"),
        icon: MapPin,
        testId: "open-branch-management",
        to: "/org/branches",
      },
      {
        key: "ownership",
        label: t("org.ownershipTransfer.tile"),
        icon: KeyRound,
        testId: "open-org-ownership-transfer",
        to: "/org/ownership-transfer",
      },
    );
  }
  if (canAdmin) {
    administrationTiles.push(
      {
        key: "devices",
        label: t("devices.listTitle"),
        icon: MonitorSmartphone,
        testId: "open-org-devices",
        to: "/org/devices",
      },
      {
        key: "cash",
        label: t("org.cashHandlingLink"),
        icon: Banknote,
        testId: "open-cash-handling",
        to: "/org/cash-handling",
      },
      {
        key: "qr",
        label: t("org.businessQr.title"),
        icon: QrCode,
        testId: "open-business-qr",
        to: "/org/business-qr",
      },
    );
  }

  const operationTiles: ActionTileDef[] = [];
  if (canSell) {
    operationTiles.push({
      key: "sell",
      label: t("experience.startSelling"),
      icon: ShoppingCart,
      testId: "open-start-selling",
      to: "/sell",
    });
  }
  if (canCatalog) {
    operationTiles.push({
      key: "catalog",
      label: t("catalog.openCatalog"),
      icon: Package,
      testId: "open-catalog",
      to: "/catalog",
    });
  }

  const workspaceTiles: ActionTileDef[] = [
    {
      key: "workspace",
      label: t("workspace.switch"),
      icon: ArrowLeftRight,
      testId: "open-switch-workspace",
      to: "/workspace",
    },
  ];

  const overview = overviewQuery.data;
  const overviewError = overviewQuery.isError
    ? describePosApiError(overviewQuery.error, t, "reports.loadError")
    : null;

  return (
    <div
      className="manager-home-page exits-page mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-3"
      data-testid="org-essentials-page"
    >
      <PageHeader
        title={t("org.title")}
        description={t("org.lede")}
        subtitle={
          boundWorkspace
            ? boundWorkspace.branchName
              ? `${boundWorkspace.organizationDisplayName} · ${boundWorkspace.branchName}`
              : boundWorkspace.organizationDisplayName
            : undefined
        }
        backTo={pageBackNav.more.to}
        backLabel={t(pageBackNav.more.labelKey)}
        backTestId="page-header-back-org"
      />

      {canDashboard ? (
        <GuideSection title={t("org.group.today")} testId="org-group-today">
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
            <>
              <div className="dashboard-metrics" data-testid="org-admin-overview">
                <DashboardHeroMetric
                  label={t("dashboard.todaySales")}
                  meta={`${overview.todaySaleCount} ${t("dashboard.transactions")}`}
                  testId="org-kpi-today-sales"
                >
                  <MoneyDisplay amount={overview.todaySalesTotal} />
                </DashboardHeroMetric>
                <div className="dashboard-metric-grid" role="list">
                  <DashboardMetricCard
                    label={t("dashboard.todayCash")}
                    icon={Banknote}
                    testId="org-kpi-today-cash"
                    tone="emphasis"
                  >
                    <MoneyDisplay amount={overview.todayCashSalesTotal} />
                  </DashboardMetricCard>
                  <DashboardMetricCard
                    label={t("dashboard.openUtang")}
                    testId="org-kpi-open-utang"
                    tone={overview.openUtangOutstanding > 0 ? "attention" : "default"}
                  >
                    <MoneyDisplay amount={overview.openUtangOutstanding} />
                  </DashboardMetricCard>
                  <DashboardMetricCard
                    label={t("dashboard.lowStock")}
                    icon={Package}
                    testId="org-kpi-low-stock"
                    tone={overview.lowStockProductCount > 0 ? "attention" : "success"}
                  >
                    {overview.lowStockProductCount}
                  </DashboardMetricCard>
                  <DashboardMetricCard
                    label={t("dashboard.openShifts")}
                    testId="org-kpi-open-shifts"
                  >
                    {overview.openShiftCount}
                  </DashboardMetricCard>
                  <DashboardMetricCard
                    label={t("dashboard.activeRegisters")}
                    testId="org-kpi-active-registers"
                  >
                    {overview.activeRegisterCount}
                  </DashboardMetricCard>
                </div>
              </div>
              <Link
                to="/dashboard"
                className="org-overview-more text-[length:var(--exits-text-sm)] font-semibold text-[var(--exits-primary)] no-underline"
                data-testid="org-overview-open-dashboard"
              >
                {t("org.overviewOpenDashboard")}
              </Link>
            </>
          ) : null}
        </GuideSection>
      ) : null}

      {insightTiles.length > 0 ? (
        <GuideSection title={t("org.group.insights")} testId="org-group-insights">
          <ActionTileGrid tiles={insightTiles} />
        </GuideSection>
      ) : null}

      {administrationTiles.length > 0 ? (
        <GuideSection title={t("org.group.administration")} testId="org-group-administration">
          <ActionTileGrid tiles={administrationTiles} />
        </GuideSection>
      ) : null}

      {operationTiles.length > 0 ? (
        <GuideSection title={t("org.group.operations")} testId="org-group-operations">
          <ActionTileGrid tiles={operationTiles} />
        </GuideSection>
      ) : null}

      <GuideSection title={t("org.group.workspace")} testId="org-group-workspace">
        <ActionTileGrid tiles={workspaceTiles} />
      </GuideSection>
    </div>
  );
}
