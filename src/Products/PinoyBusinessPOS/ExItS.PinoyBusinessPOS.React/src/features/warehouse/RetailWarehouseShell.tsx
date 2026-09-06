import type { ReactNode } from "react";
import { useEffect, useRef } from "react";
import { NavLink, Outlet, Navigate, useLocation } from "react-router-dom";
import {
  canInviteOrganizationStaff,
  canManageInventory,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { resolveRetailWarehouseNavigation } from "@/features/warehouse/retail-warehouse-gate";
import { useRetailWarehouseResolve } from "@/features/warehouse/useRetailWarehouseResolve";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const NAV_ITEMS = [
  { to: "/warehouse", end: true, labelKey: "retailWarehouse.nav.overview" as const },
  { to: "/warehouse/request-stock", end: false, labelKey: "retailWarehouse.nav.requestStock" as const },
  { to: "/warehouse/my-requests", end: false, labelKey: "retailWarehouse.nav.myRequests" as const },
  { to: "/warehouse/incoming", end: false, labelKey: "retailWarehouse.nav.incoming" as const },
  { to: "/warehouse/history", end: false, labelKey: "retailWarehouse.nav.history" as const },
];

/**
 * Retail warehouse workspace chrome. Warehouse branches are redirected to inventory stock requests.
 */
export function RetailWarehouseShell({ children }: { children?: ReactNode }) {
  const { t } = useI18n();
  const location = useLocation();
  const { showToast } = useToast();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const { resolveState, isLoading, isError } = useRetailWarehouseResolve();
  const toastedRef = useRef<string | null>(null);

  const isWarehouse = isWarehouseBranch(boundWorkspace?.branchType);

  useEffect(() => {
    if (isWarehouse || isLoading || !resolveState || resolveState.kind === "ready") return;
    const gate = resolveRetailWarehouseNavigation(
      resolveState,
      {
        canManageOrganization: hasOrganizationManagementAuthority(sessionGrant),
        canInvite: canInviteOrganizationStaff(sessionGrant),
        canManageInventory: canManageInventory(sessionGrant),
      },
      t,
      { branchName: boundWorkspace?.branchName ?? undefined },
    );
    if (gate.kind !== "toast") return;
    const key = `${location.pathname}:${resolveState.kind}`;
    if (toastedRef.current === key) return;
    toastedRef.current = key;
    showToast(gate.toast);
  }, [
    isWarehouse,
    isLoading,
    resolveState,
    sessionGrant,
    t,
    boundWorkspace?.branchName,
    showToast,
    location.pathname,
  ]);

  if (isWarehouse) {
    return <Navigate to="/inventory/stock-requests" replace />;
  }

  if (isLoading) {
    return <LoadingState label={t("retailWarehouse.loading")} />;
  }

  if (isError || !resolveState || resolveState.kind !== "ready") {
    return <Navigate to="/role/manager" replace />;
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="retail-warehouse-shell">
      <PageHeader title={t("retailWarehouse.title")} description={t("retailWarehouse.lede")} />
      <nav
        className="flex min-w-0 gap-1 overflow-x-auto pb-0.5"
        aria-label={t("retailWarehouse.navLabel")}
        data-testid="retail-warehouse-nav"
      >
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) =>
              cn(
                "shrink-0 rounded-full border px-3 py-1.5 text-[length:var(--exits-text-sm)] no-underline",
                isActive
                  ? "border-[var(--exits-primary)] bg-[color-mix(in_srgb,var(--exits-primary)_12%,var(--exits-surface))] font-medium text-foreground"
                  : "border-border bg-[var(--exits-surface)] text-muted",
              )
            }
            data-testid={`retail-warehouse-nav-${item.to.split("/").pop() || "overview"}`}
          >
            {t(item.labelKey)}
          </NavLink>
        ))}
      </nav>
      {children ?? <Outlet context={resolveState} />}
    </div>
  );
}
