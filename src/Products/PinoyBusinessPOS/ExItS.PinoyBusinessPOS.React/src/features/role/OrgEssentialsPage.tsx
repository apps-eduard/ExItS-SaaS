import { useEffect, useMemo, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { motion, useReducedMotion } from "motion/react";
import {
  AlertTriangle,
  BarChart3,
  LayoutDashboard,
  Map,
  MapPin,
  MonitorSmartphone,
  Package,
  ShieldAlert,
  Users,
  Wallet,
} from "lucide-react";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  canAccessReportsHub,
  canInviteOrganizationStaff,
  canManageStoreAreas,
  canViewDashboard,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { getManagementOverview } from "@/api/pos/pos-reporting-client";
import {
  getBranchCapacity,
  listBranchManagementSummaries,
} from "@/api/platform/organization-branches-client";
import { getOrganizationCurrentPlan } from "@/api/platform/organization-current-plan-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import { listOrganizationMembers } from "@/api/platform/organization-members-client";
import { getPosDeviceCapacity } from "@/api/platform/pos-devices-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { AdminUsageMeter } from "@/features/admin/AdminUsageMeter";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type AttentionItem = {
  id: string;
  label: string;
  value: ReactNode;
  tone: "attention" | "danger";
  to: string;
  testId: string;
};

type QuickAction = {
  id: string;
  to: string;
  label: string;
  icon: typeof MapPin;
  testId: string;
  locked?: boolean;
  lockedHint?: string;
};

/**
 * Manage Business overview (`/org`) — Owner/Admin command center.
 * Attention + today status + compact org summary. Not a second Dashboard.
 */
export function OrgEssentialsPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const reduceMotion = useReducedMotion();
  const { boundWorkspace, sessionGrant, refreshSessionGrant } = useWorkspace();
  const canDashboard = canViewDashboard(sessionGrant);
  const canReports = canAccessReportsHub(sessionGrant);
  const canInvite = canInviteOrganizationStaff(sessionGrant);
  const canAdmin = hasOrganizationManagementAuthority(sessionGrant);
  const areasEntitled = canManageStoreAreas(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;

  useEffect(() => {
    if (!organizationId || !online || !canInvite) {
      return;
    }
    void refreshSessionGrant();
  }, [organizationId, online, canInvite, refreshSessionGrant]);

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

  const branchesQuery = useQuery({
    queryKey: ["org-home-branch-summaries", organizationId],
    enabled: Boolean(organizationId && canInvite && online),
    queryFn: async ({ signal }) => {
      const result = await listBranchManagementSummaries(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "branches");
      return result.value;
    },
    staleTime: 60_000,
  });

  const branchCapacityQuery = useQuery({
    queryKey: ["org-home-branch-capacity", organizationId],
    enabled: Boolean(organizationId && canInvite && online),
    queryFn: async ({ signal }) => {
      const result = await getBranchCapacity(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "capacity");
      return result.value;
    },
    staleTime: 60_000,
  });

  const areasQuery = useQuery({
    queryKey: ["org-home-areas", organizationId],
    enabled: Boolean(organizationId && canInvite && areasEntitled && online),
    queryFn: async ({ signal }) => {
      const result = await listOrganizationAreas(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "areas");
      return result.value;
    },
    staleTime: 60_000,
  });

  const staffQuery = useQuery({
    queryKey: ["org-home-staff", organizationId],
    enabled: Boolean(organizationId && canInvite && online),
    queryFn: async () => {
      const result = await listOrganizationMembers(organizationId!, "Active");
      if (!result.ok) throw new Error(result.body?.detail ?? "staff");
      return result.members;
    },
    staleTime: 60_000,
  });

  const deviceCapacityQuery = useQuery({
    queryKey: ["org-home-device-capacity", organizationId],
    enabled: Boolean(organizationId && canAdmin && online),
    queryFn: async ({ signal }) => {
      const result = await getPosDeviceCapacity(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "devices");
      return result.value;
    },
    staleTime: 60_000,
  });

  const currentPlanQuery = useQuery({
    queryKey: ["org-home-current-plan", organizationId],
    enabled: Boolean(organizationId && canInvite && online),
    queryFn: async ({ signal }) => {
      const result = await getOrganizationCurrentPlan(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "plan");
      return result.value;
    },
    staleTime: 60_000,
  });

  const overview = overviewQuery.data;
  const overviewError = overviewQuery.isError
    ? describePosApiError(overviewQuery.error, t, "reports.loadError")
    : null;

  const locationStats = useMemo(() => {
    const branches = branchesQuery.data ?? [];
    const active = branches.filter((b) => b.status.toLowerCase() !== "inactive");
    const retail = active.filter((b) => !isWarehouseBranch(b.branchType)).length;
    const warehouse = active.filter((b) => isWarehouseBranch(b.branchType)).length;
    return { total: active.length, retail, warehouse };
  }, [branchesQuery.data]);

  const attentionItems = useMemo(() => {
    const items: AttentionItem[] = [];
    if (!overview) {
      return items;
    }
    if (overview.lowStockProductCount > 0) {
      items.push({
        id: "low-stock",
        label: t("dashboard.lowStock"),
        value: String(overview.lowStockProductCount),
        tone: "attention",
        to: "/inventory",
        testId: "org-attention-low-stock",
      });
    }
    if (overview.nearExpiryLotCount > 0) {
      items.push({
        id: "near-expiry",
        label: t("dashboard.nearExpiryLots"),
        value: String(overview.nearExpiryLotCount),
        tone: "attention",
        to: "/inventory/expiration",
        testId: "org-attention-near-expiry",
      });
    }
    if (overview.expiredLotCount > 0) {
      items.push({
        id: "expired",
        label: t("dashboard.expiredLots"),
        value: String(overview.expiredLotCount),
        tone: "danger",
        to: "/inventory/expiration",
        testId: "org-attention-expired",
      });
    }
    if (overview.openUtangOutstanding > 0) {
      items.push({
        id: "utang",
        label: t("dashboard.openUtang"),
        value: <MoneyDisplay amount={overview.openUtangOutstanding} />,
        tone: "attention",
        to: "/customers",
        testId: "org-attention-utang",
      });
    }

    const branchCap = branchCapacityQuery.data;
    if (branchCap && branchCap.allowed > 0 && branchCap.used >= branchCap.allowed) {
      items.push({
        id: "branch-capacity",
        label: t("org.attention.branchCapacity"),
        value: `${branchCap.used}/${branchCap.allowed}`,
        tone: "attention",
        to: "/org/branches",
        testId: "org-attention-branch-capacity",
      });
    }
    const deviceCap = deviceCapacityQuery.data;
    if (deviceCap && deviceCap.allowed > 0 && deviceCap.used >= deviceCap.allowed) {
      items.push({
        id: "device-capacity",
        label: t("org.attention.deviceCapacity"),
        value: `${deviceCap.used}/${deviceCap.allowed}`,
        tone: "attention",
        to: "/org/devices",
        testId: "org-attention-device-capacity",
      });
    }
    const areas = areasQuery.data;
    if (areas && areas.maxAreas > 0 && areas.activeAreaCount >= areas.maxAreas) {
      items.push({
        id: "area-capacity",
        label: t("org.attention.areaCapacity"),
        value: `${areas.activeAreaCount}/${areas.maxAreas}`,
        tone: "attention",
        to: "/org/areas",
        testId: "org-attention-area-capacity",
      });
    }
    return items;
  }, [
    overview,
    branchCapacityQuery.data,
    deviceCapacityQuery.data,
    areasQuery.data,
    t,
  ]);

  const quickActions = useMemo(() => {
    const actions: QuickAction[] = [];
    if (canInvite) {
      actions.push({
        id: "branches",
        to: "/org/branches",
        label: t("admin.nav.branchesWarehouses"),
        icon: MapPin,
        testId: "org-action-branches",
      });
      actions.push({
        id: "staff",
        to: "/org/staff",
        label: t("admin.nav.staff"),
        icon: Users,
        testId: "org-action-staff",
      });
      if (areasEntitled) {
        actions.push({
          id: "areas",
          to: "/org/areas",
          label: t("admin.nav.areas"),
          icon: Map,
          testId: "org-action-areas",
        });
      } else {
        actions.push({
          id: "areas",
          to: "/org/areas",
          label: t("admin.nav.areas"),
          icon: Map,
          testId: "org-action-areas",
          locked: true,
          lockedHint: t("admin.nav.lockedPro"),
        });
      }
    }
    if (canDashboard) {
      actions.push({
        id: "dashboard",
        to: "/dashboard",
        label: t("dashboard.open"),
        icon: LayoutDashboard,
        testId: "open-org-dashboard",
      });
    }
    if (canReports) {
      actions.push({
        id: "reports",
        to: "/reports",
        label: t("reports.open"),
        icon: BarChart3,
        testId: "open-org-reports",
      });
    }
    return actions.slice(0, 5);
  }, [canInvite, areasEntitled, canDashboard, canReports, t]);

  const fade = reduceMotion
    ? undefined
    : { initial: { opacity: 0, y: 6 }, animate: { opacity: 1, y: 0 } };

  const showPlanSection = Boolean(
    currentPlanQuery.data?.planDisplayName ||
      branchCapacityQuery.data ||
      (areasEntitled && areasQuery.data && areasQuery.data.maxAreas > 0) ||
      deviceCapacityQuery.data,
  );

  return (
    <div
      className="admin-overview-page admin-overview-page--v2 exits-page mx-auto flex w-full max-w-[1200px] min-w-0 flex-col gap-4"
      data-testid="org-essentials-page"
    >
      <PageHeader
        title={t("admin.shell.manageBusiness")}
        description={t("org.lede")}
        subtitle={boundWorkspace?.organizationDisplayName}
      />

      <motion.div
        className="admin-command flex flex-col gap-4"
        {...(fade ?? {})}
        transition={{ duration: 0.3, ease: [0.22, 1, 0.36, 1] }}
      >
        {canDashboard || showPlanSection ? (
          <div className="admin-overview-top" data-testid="org-overview-top">
            {canDashboard ? (
              <section className="admin-command__section" data-testid="org-group-today">
                <h2 className="admin-command__title m-0">{t("org.group.today")}</h2>
                {!online ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid="org-overview-offline"
                  >
                    {t("org.overviewOffline")}
                  </p>
                ) : null}
                {online && overviewQuery.isLoading ? (
                  <LoadingState label={t("reports.loading")} />
                ) : null}
                {online && overviewError ? (
                  <ErrorState title={t("reports.errorTitle")} detail={overviewError} />
                ) : null}
                {online && overview ? (
                  <div className="admin-today" data-testid="org-admin-overview">
                    <article className="admin-today__hero" data-testid="org-kpi-today-sales">
                      <span className="admin-today__label">{t("dashboard.todaySales")}</span>
                      <div className="admin-today__value">
                        <MoneyDisplay amount={overview.todaySalesTotal} />
                      </div>
                      {overview.todaySalesTotal <= 0 ? (
                        <p className="admin-today__empty m-0">{t("org.today.noSales")}</p>
                      ) : (
                        <p className="admin-today__meta m-0">
                          {overview.todaySaleCount} {t("dashboard.transactions")}
                        </p>
                      )}
                    </article>
                    <div className="admin-today__strip" role="list">
                      <div
                        className="admin-today__chip"
                        data-testid="org-kpi-open-utang"
                        role="listitem"
                      >
                        <span className="admin-today__chip-label">{t("dashboard.openUtang")}</span>
                        <span
                          className={cn(
                            "admin-today__chip-value",
                            overview.openUtangOutstanding > 0 &&
                              "admin-today__chip-value--attention",
                          )}
                        >
                          <MoneyDisplay amount={overview.openUtangOutstanding} />
                        </span>
                      </div>
                      <div
                        className="admin-today__chip"
                        data-testid="org-kpi-low-stock"
                        role="listitem"
                      >
                        <span className="admin-today__chip-label">{t("dashboard.lowStock")}</span>
                        <span
                          className={cn(
                            "admin-today__chip-value",
                            overview.lowStockProductCount > 0 &&
                              "admin-today__chip-value--attention",
                          )}
                        >
                          {overview.lowStockProductCount}
                        </span>
                      </div>
                      <div
                        className="admin-today__chip"
                        data-testid="org-kpi-open-shifts"
                        role="listitem"
                      >
                        <span className="admin-today__chip-label">{t("dashboard.openShifts")}</span>
                        <span className="admin-today__chip-value">{overview.openShiftCount}</span>
                      </div>
                    </div>
                  </div>
                ) : null}
              </section>
            ) : null}

            {showPlanSection ? (
              <section className="admin-command__section" data-testid="org-group-plan">
                <div className="admin-plan-header flex min-w-0 items-center justify-between gap-2">
                  <h2 className="admin-command__title m-0">{t("org.group.plan")}</h2>
                  {currentPlanQuery.data?.planDisplayName ? (
                    <span data-testid="org-plan-chip">
                      <StatusChip tone="info">{currentPlanQuery.data.planDisplayName}</StatusChip>
                    </span>
                  ) : null}
                </div>
                <div className="admin-plan-usage" data-testid="org-plan-usage">
                  <h3 className="admin-plan-usage__title m-0">{t("org.plan.usageTitle")}</h3>
                  <ul className="admin-plan-usage__meters m-0 list-none p-0">
                    {branchCapacityQuery.data ? (
                      <AdminUsageMeter
                        label={t("admin.context.branches")}
                        used={branchCapacityQuery.data.used}
                        allowed={branchCapacityQuery.data.allowed}
                        testId="org-plan-capacity-branches"
                      />
                    ) : null}
                    {areasEntitled && areasQuery.data && areasQuery.data.maxAreas > 0 ? (
                      <AdminUsageMeter
                        label={t("admin.context.areas")}
                        used={areasQuery.data.activeAreaCount}
                        allowed={areasQuery.data.maxAreas}
                        testId="org-plan-capacity-areas"
                      />
                    ) : null}
                    {deviceCapacityQuery.data ? (
                      <AdminUsageMeter
                        label={t("admin.context.devices")}
                        used={deviceCapacityQuery.data.used}
                        allowed={deviceCapacityQuery.data.allowed}
                        testId="org-plan-capacity-devices"
                      />
                    ) : null}
                  </ul>
                </div>
              </section>
            ) : null}
          </div>
        ) : null}

        <section className="admin-command__section" data-testid="org-attention-section">
          <h2 className="admin-command__title m-0">{t("org.attention.title")}</h2>
          {attentionItems.length === 0 ? (
            <p className="admin-attention-clear m-0" data-testid="org-attention-clear">
              {t("org.attention.clear")}
            </p>
          ) : (
            <ul className="admin-attention-list m-0 list-none p-0">
              {attentionItems.map((item) => (
                <li key={item.id}>
                  <Link
                    to={item.to}
                    className={cn(
                      "admin-attention-item",
                      item.tone === "danger" && "admin-attention-item--danger",
                      item.tone === "attention" && "admin-attention-item--attention",
                    )}
                    data-testid={item.testId}
                  >
                    <span className="admin-attention-item__icon" aria-hidden>
                      {item.tone === "danger" ? (
                        <ShieldAlert className="size-4" />
                      ) : item.id === "utang" ? (
                        <Wallet className="size-4" />
                      ) : item.id === "low-stock" ? (
                        <Package className="size-4" />
                      ) : (
                        <AlertTriangle className="size-4" />
                      )}
                    </span>
                    <span className="admin-attention-item__label">{item.label}</span>
                    <span className="admin-attention-item__value">{item.value}</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="admin-command__section" data-testid="org-glance-section">
          <h2 className="admin-command__title m-0">{t("org.glance.title")}</h2>
          <div className="admin-glance-grid" role="list">
            {canInvite ? (
              <div className="admin-glance-card" data-testid="org-glance-locations" role="listitem">
                <span className="admin-glance-card__label">{t("org.glance.locations")}</span>
                <span className="admin-glance-card__value">
                  {branchesQuery.isLoading ? "…" : locationStats.total}
                </span>
                <span className="admin-glance-card__meta">
                  {t("org.glance.locationsBreakdown")
                    .replace("{retail}", String(locationStats.retail))
                    .replace("{warehouse}", String(locationStats.warehouse))}
                </span>
              </div>
            ) : null}
            {canInvite && areasEntitled ? (
              <div className="admin-glance-card" data-testid="org-glance-areas" role="listitem">
                <span className="admin-glance-card__label">{t("org.glance.areas")}</span>
                <span className="admin-glance-card__value">
                  {areasQuery.isLoading ? "…" : (areasQuery.data?.activeAreaCount ?? 0)}
                </span>
              </div>
            ) : null}
            {canInvite ? (
              <div className="admin-glance-card" data-testid="org-glance-staff" role="listitem">
                <span className="admin-glance-card__label">{t("org.glance.staff")}</span>
                <span className="admin-glance-card__value">
                  {staffQuery.isLoading ? "…" : (staffQuery.data?.length ?? 0)}
                </span>
              </div>
            ) : null}
            {canAdmin ? (
              <div className="admin-glance-card" data-testid="org-glance-devices" role="listitem">
                <span className="admin-glance-card__label">{t("org.glance.devices")}</span>
                <span className="admin-glance-card__value">
                  {deviceCapacityQuery.isLoading
                    ? "…"
                    : (deviceCapacityQuery.data?.used ?? 0)}
                </span>
                {deviceCapacityQuery.data ? (
                  <span className="admin-glance-card__meta inline-flex items-center gap-1">
                    <MonitorSmartphone className="size-3.5 opacity-60" aria-hidden />
                    {deviceCapacityQuery.data.used}/{deviceCapacityQuery.data.allowed}
                  </span>
                ) : null}
              </div>
            ) : null}
          </div>
        </section>

        {quickActions.length > 0 ? (
          <section className="admin-command__section" data-testid="org-quick-actions">
            <h2 className="admin-command__title m-0">{t("org.actions.title")}</h2>
            <div className="admin-quick-actions">
              {quickActions.map((action) => {
                const Icon = action.icon;
                return (
                  <Link
                    key={action.id}
                    to={action.to}
                    className={cn(
                      "admin-quick-action",
                      action.locked && "admin-quick-action--locked",
                    )}
                    data-testid={action.testId}
                  >
                    <Icon className="size-4 shrink-0" aria-hidden />
                    <span className="min-w-0">
                      <span className="block font-medium">{action.label}</span>
                      {action.locked && action.lockedHint ? (
                        <span className="block text-[length:var(--exits-text-xs)] text-muted">
                          {action.lockedHint}
                        </span>
                      ) : null}
                    </span>
                  </Link>
                );
              })}
            </div>
          </section>
        ) : null}
      </motion.div>
    </div>
  );
}
